using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using voxelgame.scripts;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;
using WorldDataFloat = FastWorldData<WorldSettings1, float>;

public class BadLightEngine {
    private static readonly Vector3T<int>[] dirs = new Vector3T<int>[]{
		//p1
		new( 1, 0, 0), new( 0, 1, 0), new( 0, 0, 1),
        new(-1, 0, 0), new( 0,-1, 0), new( 0, 0,-1),
		//p2
		new( 1, 1, 0), new( 0, 1, 1), new( 1, 0, 1),
        new(-1, 1, 0), new( 0,-1, 1), new(-1, 0, 1),
        new( 1,-1, 0), new( 0, 1,-1), new( 1, 0,-1),
        new(-1,-1, 0), new( 0,-1,-1), new(-1, 0,-1),
		//p3
		new( 1, 1, 1), new( 1, 1,-1), new( 1,-1, 1), new( 1,-1,-1),
        new(-1, 1, 1), new(-1, 1,-1), new(-1,-1, 1), new(-1,-1,-1),
    };

    private readonly World world;
    private readonly VoxelEngine engine;

    private readonly ConcurrentQueue<LightUpdateOrder> nextsources = new();
    private readonly ConcurrentQueue<LightUpdateMergeRequest> mergequeue = new();

    private long mainWorkerId;
    public bool Enabled { get; set; } = true;


    public BadLightEngine(World nworld, VoxelEngine nengine) {
        world = nworld;
        engine = nengine;
    }

    public void AsyncStartLighting() {
        mainWorkerId = WorkerThreadPool.AddTask(Callable.From(ComputeLighting));
    }

    public void ComputeLighting() {
        if (!Enabled) { return; }

        //bool[] empty = new bool[] { true };
        //world.Voxels.ForAll((xyz) => { empty[0] &= world.Voxels[xyz].color == 0; });
        //GD.Print($"empty={empty[0]}");

        GD.Print("Initiating light maps");
        WorldDataVec3 currentmap = new((wind, cind) => world.Voxels[wind, cind].color);
        WorldDataVec3 updatemap = new((wind, cind) => currentmap[wind, cind]);

        nextsources.Enqueue(new(new(+02, +02, -9), RandomColor() * 680, new(0)));
        //nextsources.Enqueue(new(new(+11, +62, 10), RandomColor() * 330, new(0)));
        //nextsources.Enqueue(new(new(+73, -12, 13), RandomColor() * 500, new(0)));
        //nextsources.Enqueue(new(new(-50, -38, -3), RandomColor() * 300, new(0)));

        GD.Print("starting the light computation");
        while (true) {
            GD.Print("finding significant sources");
            QueueSignificantSources(updatemap, currentmap);
            var ids = StartAsyncLightmapsComputing();
            GD.Print($"sources: {ids.Count}");
            if (ids.Count == 0) { break; }
            foreach (var id in ids) {
                //GD.Print($"waiting on task {id}");
                WorkerThreadPool.WaitForTaskCompletion(id);
            }
            if (!Enabled) { return; }
            GD.Print("applying results");
            if (ApplyMergeQueue(currentmap)) {
                GD.Print("preparing layers");
                engine.PrepareColorLayers(currentmap);
            }
        }
        GD.Print("no more sources");
    }

    public void Stop() {
        GD.Print("stopping light updates");
        Enabled = false;
        WorkerThreadPool.WaitForTaskCompletion(mainWorkerId);
    }

    public void QueueSignificantSources(WorldDataVec3 updatemap, WorldDataVec3 currentmap) {
        float[] top = new float[] { 0.05f };
        List<LightUpdateOrder> orders = new();

        updatemap.ForAll((xyz) => {
            world.Voxels.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var emit = currentmap[wind, cind] - updatemap[wind, cind];
            if (emit == 0) { return; }

            var adjs = world.Adjacency[wind, cind];
            if (adjs.IsEmpty()) { return; }
            var nb1 = adjs[0] ? world.Voxels[xyz + (1, 0, 0)].color : new(0);
            var nb2 = adjs[1] ? world.Voxels[xyz + (0, 1, 0)].color : new(0);
            var nb3 = adjs[2] ? world.Voxels[xyz + (0, 0, 1)].color : new(0);
            var nb4 = adjs[3] ? world.Voxels[xyz - (1, 0, 0)].color : new(0);
            var nb5 = adjs[4] ? world.Voxels[xyz - (0, 1, 0)].color : new(0);
            var nb6 = adjs[5] ? world.Voxels[xyz - (0, 0, 1)].color : new(0);
            var bestnb = nb1.Max(nb2).Max(nb3).Max(nb4).Max(nb5).Max(nb6);
            emit *= bestnb;

            float emax = emit.Max();
            if (top[0] * 0.6f <= emax) {
                if (top[0] <= emax) {
                    top[0] = Math.Max(top[0], emax);
                }

                Vector3T<int> filter = new(0) {
                    X = adjs.Get(0) - adjs.Get(3),
                    Y = adjs.Get(1) - adjs.Get(4),
                    Z = adjs.Get(2) - adjs.Get(5)
                };
                orders.Add(new(xyz, emit, filter));
            }
        });
        GD.Print($"top emax={top[0]}");

        int it = 0;
        foreach (var order in orders.OrderBy((o) => -o.Emit.Max())) {
            if (20 <= it) { break; }
            //GD.Print($"enqueued source emax={order.Emit.Max()}");
            nextsources.Enqueue(order);
            updatemap[order.Source] = currentmap[order.Source];
            it++;
        }
    }

    public List<long> StartAsyncLightmapsComputing() {
        List<long> ids = new();
        while (nextsources.TryDequeue(out LightUpdateOrder? rslt)) {
            var source = rslt.Source;
            var emit = rslt.Emit;
            var filter = rslt.Filter;
            long id = WorkerThreadPool.AddTask(Callable.From(() => ComputeOneLightmap(source, emit, filter)));
            ids.Add(id);
        }
        return ids;
    }

    public void ComputeOneLightmap(Vector3T<int> source, Vector3T<float> emit, Vector3T<int> filter) {
        WorldDataVec3 vmap = ComputeVisibility(source, filter);
        WorldDataVec3 cmap = ComputeLightmap(vmap, source, emit, filter);

        mergequeue.Enqueue(new(cmap));
    }

    public WorldDataVec3 ComputeVisibility(Vector3T<int> pos, Vector3T<int> filter) {//vec3 dir, decayrate
        WorldDataVec3 vmap = WorldDataVec3.UnsafeNew();
        var mins = vmap.Settings.TotalMins;
        var maxs = vmap.Settings.TotalMaxs;

        vmap[pos] = new(1f);

        foreach (var dir in dirs) {
            bool filtered;
            //intersection excl filter
            filtered = filter.Any(dir, (v, d) => v != 0 && v != d);
            //intersection incl filter
            // filter.Any(dir, (v, d) => v != 0 && v == -d); 
            //union incl filter
            // !(filter == 0) && filter.All(dir, (v, d) => v == 0 || v == -d); 
            if (filtered) {
                //continue;
            }

            var adir = dir.Abs();
            var spos = pos + dir;
            var maxs2 = dir.Do(maxs, spos, (d, max, spo) => (d == 0) ? spo : max);
            var mins2 = dir.Do(mins, spos, (d, min, spo) => (d == 0) ? spo : min);

            var incr = dir.Do((val) => (0 <= val) ? 1 : -1);
            for (int itx = spos.X; IsInBoundII(itx, mins2.X, maxs2.X); itx += incr.X) {
                for (int ity = spos.Y; IsInBoundII(ity, mins2.Y, maxs2.Y); ity += incr.Y) {
                    for (int itz = spos.Z; IsInBoundII(itz, mins2.Z, maxs2.Z); itz += incr.Z) {
                        Vector3T<int> xyz = new(itx, ity, itz);
                        vmap.DeconstructPosToIndex(xyz, out var wind, out var cind);
                        if (world.Occupancy[wind, cind]) { continue; }

                        var dist = pos.DistanceTo(xyz);
                        var vx = vmap[new(itx - dir.X, ity, itz)].X * dist.X;
                        var vy = vmap[new(itx, ity - dir.Y, itz)].X * dist.Y;
                        var vz = vmap[new(itx, ity, itz - dir.Z)].X * dist.Z;

                        vmap[wind, cind] = new((vx + vy + vz) / dist.Sum(), 0, 0);
                    }
                }
            }

        }
        return vmap;
    }

    public static bool IsInBoundII(int pos, int min, int max) {
        return min <= pos && pos <= max;
    }

    public WorldDataVec3 ComputeLightmap(WorldDataVec3 vmap, Vector3T<int> source, Vector3T<float> emit, Vector3T<int> filter) {
        WorldDataVec3 lmap = vmap;
        lmap.ForAll((xyz) => {// light_color * light_intensity * GBV_value * lambert / distance^2
            if (xyz == source) { return; }
            lmap.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (world.Occupancy[wind, cind]) { return; }

            float lval = vmap[wind, cind].X;
            vmap[wind, cind] = new(0);
            lval = Mathf.Clamp(lval * 10 - 5, 0, 1);
            if (lval == 0) { return; }

            var dist = xyz - source;
            if (dist == 0) { return; }

            var adjs = world.Adjacency[wind, cind]; //TODO also do source (filter) lambert
            if (adjs.IsEmpty()) { return; }
            var l1 = adjs[0] ? GetLambert(dist, new(1, 0, 0), filter) : 0;
            var l2 = adjs[1] ? GetLambert(dist, new(0, 1, 0), filter) : 0;
            var l3 = adjs[2] ? GetLambert(dist, new(0, 0, 1), filter) : 0;
            var l4 = adjs[3] ? GetLambert(dist, new(-1, 0, 0), filter) : 0;
            var l5 = adjs[4] ? GetLambert(dist, new(0, -1, 0), filter) : 0;
            var l6 = adjs[5] ? GetLambert(dist, new(0, 0, -1), filter) : 0;
            //float avglambert = (l1 + l2 + l3 + l4 + l5 + l6) / adjs.Sum(); //TODO remove adjs that arent facing the ray
            float bestlambert = Math.Max(Math.Max(Mathf.Max(l1, l2), Mathf.Max(l3, l4)), Mathf.Max(l5, l6));

            lval /= dist.Square().Sum() + 1;
            var lcol = lval * emit * 1.0f;
            lcol *= bestlambert;
            lmap[wind, cind] = lcol;
        });
        return lmap;
    }

    public static float GetLambert(Vector3T<int> tdist, Vector3T<int> tnormal, Vector3T<int> tfilter) {
        if (tfilter == tnormal) {
            return 0;
        }
        tdist += tnormal;//.Do(tfilter, (n, f) => (n == f) ? n : (n - f));

        Vector3 normal = tnormal.ToVector3();
        Vector3 dist = tdist.ToVector3();
        dist = dist.Normalized();

        //Ax* Bx +Ay * By + Az * Bz
        var mult = dist * normal;
        return Math.Max(mult.X + mult.Y + mult.Z, 0);
    }


    public bool ApplyMergeQueue(WorldDataVec3 currentmap) {
        bool updated = false;
        while (mergequeue.TryDequeue(out LightUpdateMergeRequest? rslt)) {
            MergeColormap(currentmap, rslt.Cmap);
            updated = true;
        }
        return updated;
    }

    public static void MergeColormap(WorldDataVec3 currentmap, WorldDataVec3 comap) {
        currentmap.ForAll((wind, cind) => {
            var color = comap[wind, cind];
            if (color == 0) { return; }
            currentmap[wind, cind] += color;
        });
    }

    public static Vector3T<float> RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());
}

public record class LightUpdateResult(WorldDataVec3 Cmap);
public record class LightUpdateMergeRequest(WorldDataVec3 Cmap);
public record class LightUpdateOrder(Vector3T<int> Source, Vector3T<float> Emit, Vector3T<int> Filter);

