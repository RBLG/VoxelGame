using Godot;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using voxelgame.scripts;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;
using WorldDataFloat = FastWorldData<WorldSettings1, float>;


public class BadLightEngine {
    private readonly World world;
    private readonly VoxelEngine engine;

    public BadLightEngine(World nworld, VoxelEngine nengine) {
        world = nworld;
        engine = nengine;
    }

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

    public bool Enabled { get; set; } = true;

    public void AsyncDoLighting() {
        mainWorkerId = WorkerThreadPool.AddTask(Callable.From(DoLighting));
        //Parallel.Invoke(DoLighting);
    }

    private readonly ConcurrentQueue<LightUpdateOrder> nextsources = new();
    private readonly ConcurrentQueue<LightUpdateMergeRequest> mergequeue = new();



    public static Vector3T<float> RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());

    public void DoLighting() {
        if (!Enabled) { return; }
        GD.Print("Initiating light maps");
        WorldDataVec3 currentmap = new((wind, cind) => world.chunks[wind, cind].color);
        WorldDataVec3 updatemap = new((wind, cind) => currentmap[wind, cind]);
        var p = new Vector3T<int>(0);
        var p1 = p + (+02, +02, -9);
        var p2 = p + (+11, +62, 10);
        var p3 = p + (+73, -12, 13);
        var p4 = p + (-50, -38, -3);
        currentmap[p1] += RandomColor() * 680;
        currentmap[p2] += RandomColor() * 330;
        currentmap[p3] += RandomColor() * 500;
        currentmap[p4] += RandomColor() * 300;
        nextsources.Enqueue(new(p1, currentmap[p1] - updatemap[p1], new(0)));
        nextsources.Enqueue(new(p2, currentmap[p2] - updatemap[p2], new(0)));
        nextsources.Enqueue(new(p3, currentmap[p3] - updatemap[p3], new(0)));
        nextsources.Enqueue(new(p4, currentmap[p4] - updatemap[p4], new(0)));
        updatemap = new((wind, cind) => currentmap[wind, cind]);


        GD.Print("preparing the raw color layer");
        engine.PrepareColorLayers(currentmap);

        GD.Print("starting the light computation");
        while (true) {
            GD.Print("finding significant sources");
            QueueSignificantSources(updatemap, currentmap);
            var ids = ContinueDoingLighting();
            GD.Print($"sources: {ids.Count}");
            if (ids.Count == 0) { break; }
            foreach (var id in ids) {
                WorkerThreadPool.WaitForTaskCompletion(id);
            }
            if (!Enabled) { return; }
            GD.Print("applying results");
            if (ApplyMergeQueue(currentmap)) {
                engine.PrepareColorLayers(currentmap);
            }
        }
        GD.Print("no more sources");
    }

    public LightUpdateOrder[] currentorders = Array.Empty<LightUpdateOrder>();

    public List<long> ContinueDoingLighting() {
        List<long> ids = new();
        while (nextsources.TryDequeue(out LightUpdateOrder? rslt)) {
            var source = rslt.Source;
            var emit = rslt.Emit;
            var filter = rslt.Filter;
            long id = WorkerThreadPool.AddTask(Callable.From(() => DoLightingOnce(source, emit, filter)));
            ids.Add(id);
        }
        return ids;
    }



    public bool ApplyMergeQueue(WorldDataVec3 currentmap) {
        bool updated = false;
        while (mergequeue.TryDequeue(out LightUpdateMergeRequest? rslt)) {
            MergeColormap(currentmap, rslt.Cmap);
            updated = true;
        }
        return updated;
    }

    public void QueueSignificantSources(WorldDataVec3 updatemap, WorldDataVec3 currentmap) {

        float[] top = new float[] { 0.05f };
        List<LightUpdateOrder> orders = new();


        updatemap.ForAll((xyz) => {
            world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var emit = currentmap[wind, cind] - updatemap[wind, cind];

            if (emit == 0) { return; }
            var adjs = world.Adjacency[wind, cind];
            if (adjs.IsEmpty()) { return; }
            var nb1 = adjs[0] ? world.chunks[xyz + (1, 0, 0)].color : new(0);
            var nb2 = adjs[1] ? world.chunks[xyz + (0, 1, 0)].color : new(0);
            var nb3 = adjs[2] ? world.chunks[xyz + (0, 0, 1)].color : new(0);
            var nb4 = adjs[3] ? world.chunks[xyz - (1, 0, 0)].color : new(0);
            var nb5 = adjs[4] ? world.chunks[xyz - (0, 1, 0)].color : new(0);
            var nb6 = adjs[5] ? world.chunks[xyz - (0, 0, 1)].color : new(0);
            var bestnb = nb1.Max(nb2).Max(nb3).Max(nb4).Max(nb5).Max(nb6);
            emit *= bestnb;




            float emax = emit.Max();
            if (0.05f <= emax && top[0] * 0.7f <= emax) {
                top[0] = Math.Max(top[0], emax);

                Vector3T<int> filter = new(0) {
                    X = adjs.Get(0) - adjs.Get(3),
                    Y = adjs.Get(1) - adjs.Get(4),
                    Z = adjs.Get(2) - adjs.Get(5)
                };
                orders.Add(new(xyz, emit, filter));
            }
        });
        int it = 0;
        foreach (var order in orders.OrderBy((o) => o.Emit.Max())) {
            if (8 <= it) { break; }
            nextsources.Enqueue(order);
            updatemap[order.Source] = currentmap[order.Source];
            it++;
        }
    }

    public void DoLightingOnce(Vector3T<int> source, Vector3T<float> emit, Vector3T<int> filter) {
        WorldDataFloat lmap = WorldDataFloat.UnsafeNew();

        DoSourceLighting(source, lmap, filter);
        WorldDataVec3 cmap = WorldDataVec3.UnsafeNew();
        ApplyLightmap(cmap, lmap, source, emit, filter);

        mergequeue.Enqueue(new(cmap));
    }

    public void DoSourceLighting(Vector3T<int> pos, WorldDataFloat lmap, Vector3T<int> filter) {//vec3 dir, decayrate
        var mins = world.chunks.Mins;
        var maxs = world.chunks.Maxs;

        lmap[pos] = 1f;

        foreach (var dir in dirs) {
            bool filtered;
            //intersection excl filter
            filtered = filter.Any(dir, (v, d) => v != 0 && v != d);
            //intersection incl filter
            // filter.Any(dir, (v, d) => v != 0 && v == -d); 
            //union incl filter
            // !(filter == 0) && filter.All(dir, (v, d) => v == 0 || v == -d); 
            if (filtered) {
                continue;
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
                        world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);
                        if (world.Opacity[wind, cind]) {
                            continue;
                        }
                        var dist = pos.DistanceTo(xyz);
                        var vx = lmap[new(itx - dir.X, ity, itz)] * dist.X;
                        var vy = lmap[new(itx, ity - dir.Y, itz)] * dist.Y;
                        var vz = lmap[new(itx, ity, itz - dir.Z)] * dist.Z;

                        lmap[wind, cind] = (vx + vy + vz) / dist.Sum();
                    }
                }
            }

        }

    }

    public static bool IsInBoundII(int pos, int min, int max) {
        return min <= pos && pos <= max;
    }

    public void ApplyLightmap(WorldDataVec3 cmap, WorldDataFloat lmap, Vector3T<int> source, Vector3T<float> emit, Vector3T<int> filter) {
        //var sadjs = world.Adjacency[source];
        //bool isface=sadjs.Sum()==1;

        cmap.ForAll((xyz) => {// light_color * light_intensity * GBV_value * lambert / distance^2
            if (xyz == source) { return; }
            world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (world.Opacity[wind, cind]) { return; }

            float lval = lmap[wind, cind];
            if (lval == 0) { return; }

            var dist = xyz - source;
            if (dist == 0) { return; }

            var adjs = world.Adjacency[wind, cind]; //TODO also do source (filter) lambert
            if (adjs.IsEmpty()) { return; }
            var l1 = 0 <= dist.X && adjs[0] ? GetLambert(dist, new(1, 0, 0), filter) : 0;
            var l2 = 0 <= dist.Y && adjs[1] ? GetLambert(dist, new(0, 1, 0), filter) : 0;
            var l3 = 0 <= dist.Z && adjs[2] ? GetLambert(dist, new(0, 0, 1), filter) : 0;
            var l4 = 0 >= dist.X && adjs[3] ? GetLambert(dist, new(-1, 0, 0), filter) : 0;
            var l5 = 0 >= dist.Y && adjs[4] ? GetLambert(dist, new(0, -1, 0), filter) : 0;
            var l6 = 0 >= dist.Z && adjs[5] ? GetLambert(dist, new(0, 0, -1), filter) : 0;
            //float avglambert = (l1 + l2 + l3 + l4 + l5 + l6) / adjs.Sum();
            float bestlambert = Math.Max(Math.Max(Mathf.Max(l1, l2), Mathf.Max(l3, l4)), Mathf.Max(l5, l6));

            //lval = (lval < 0.5) ? 0 : 1;
            //lval /= MathF.Sqrt(dist.Square().Sum());
            lval /= (dist - filter).Square().Sum();
            var lcol = lval * emit * 0.6f;
            //lcol *= bestlambert;
            cmap[wind, cind] = lcol;
            if (cmap[wind, cind] != lcol) {
                GD.Print("ApplyLightmap failed to apply");
            }
            //if (lmap[wind, cind] != 0 && bestlambert==0) {
            //    GD.Print("ApplyLightmap went wrong in light effects");
            //}
        });
    }

    public static float GetLambert(Vector3T<int> tdist, Vector3T<int> tnormal, Vector3T<int> tfilter) {
        //if (!world.Opacity[xyz + tnormal]) { return 0; }

        if (!(tfilter == tnormal)) {
            tdist += tnormal.Do(tfilter, (n, f) => (n == f) ? n : (n - f));
        }

        Vector3 normal = new(tnormal.X, tnormal.Y, tnormal.Z);
        Vector3 dist = new(tdist.X, tdist.Y, tdist.Z);
        dist = dist.Normalized();


        //Ax* Bx +Ay * By + Az * Bz
        var mult = dist * normal;
        var lambert = Math.Max(mult.X + mult.Y + mult.Z, 0);

        return lambert;
    }

    public void MergeColormap(WorldDataVec3 currentmap, WorldDataVec3 comap) {
        currentmap.ForAll((wind, cind) => {
            currentmap[wind, cind] += comap[wind, cind];
        });
    }

    private long mainWorkerId;

    public void WaitEnd() {
        Enabled = false;
        WorkerThreadPool.WaitForTaskCompletion(mainWorkerId);
    }
}

public record class LightUpdateResult(WorldDataVec3 Cmap);
public record class LightUpdateMergeRequest(WorldDataVec3 Cmap);
public record class LightUpdateOrder(Vector3T<int> Source, Vector3T<float> Emit, Vector3T<int> Filter);

