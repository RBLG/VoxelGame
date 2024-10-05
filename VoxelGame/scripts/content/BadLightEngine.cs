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

using WorldDataVec3 = WorldData1<Vector3T<float>>;
using WorldDataFloat = WorldData1<float>;


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
    //private readonly ConcurrentQueue<LightUpdateResult> changequeue = new();



    public static Vector3T<float> RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());

    public void DoLighting() {
        if (!Enabled) {
            return;
        }
        GD.Print("Initiating light maps");
        WorldDataVec3 maincmap = new((wind, cind) => world.chunks[wind, cind].color);
        WorldDataVec3 emitmap = WorldDataVec3.UnsafeNew();
        var p = new Vector3T<int>();
        var p1 = p + (+02, +02, -9);
        var p2 = p + (+11, +62, 10);
        var p3 = p + (+73, -12, 13);
        var p4 = p + (-50, -38, -3);
        maincmap[p1] = RandomColor() * 680;
        maincmap[p2] = RandomColor() * 330;
        maincmap[p3] = RandomColor() * 500;
        maincmap[p4] = RandomColor() * 300;
        nextsources.Enqueue(new(p1, maincmap[p1], new(0)));
        nextsources.Enqueue(new(p2, maincmap[p2], new(0)));
        nextsources.Enqueue(new(p3, maincmap[p3], new(0)));
        nextsources.Enqueue(new(p4, maincmap[p4], new(0)));

        GD.Print("preparing the raw color layer");
        engine.PrepareColorLayers(maincmap);

        GD.Print("starting the light computation");
        bool done = false;
        while (!done) {
            QueueSignificantSources(emitmap);
            var ids = ContinueDoingLighting();
            done = true;
            GD.Print($"sources: {ids.Count}");
            foreach (var id in ids) {
                done = false;
                WorkerThreadPool.WaitForTaskCompletion(id);
            }
            if (!Enabled) {
                return;
            }
            if (ApplyMergeQueue(maincmap, emitmap)) {
                engine.PrepareColorLayers(maincmap);
                //changequeue.Enqueue(new(maincmap));
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



    public bool ApplyMergeQueue(WorldDataVec3 mcmap, WorldDataVec3 changemap) {
        bool updated = false;
        while (mergequeue.TryDequeue(out LightUpdateMergeRequest? rslt)) {
            MergeColormap(mcmap, changemap, rslt.Cmap);
            updated = true;
        }
        return updated;
    }


    /*public bool ApplyLatestResults() {
        bool updated = false;
        while (changequeue.TryDequeue(out LightUpdateResult? rslt)) {
            //ApplyColormap(rslt.Cmap);
            updated = true;
        }
        return updated;
    }*/

    public void QueueSignificantSources(WorldDataVec3 changemap) {

        float[] top = new float[] { 0.05f };
        List<LightUpdateOrder> orders = new();


        changemap.ForAll((xyz) => {
            world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var emit = changemap[wind, cind];

            var adjs = world.Adjacency[wind, cind];
            var nb1 = adjs[0] ? world.chunks[xyz + (1, 0, 0)].color : new(0);
            var nb2 = adjs[1] ? world.chunks[xyz + (0, 1, 0)].color : new(0);
            var nb3 = adjs[2] ? world.chunks[xyz + (0, 0, 1)].color : new(0);
            var nb4 = adjs[3] ? world.chunks[xyz - (1, 0, 0)].color : new(0);
            var nb5 = adjs[4] ? world.chunks[xyz - (0, 1, 0)].color : new(0);
            var nb6 = adjs[5] ? world.chunks[xyz - (0, 0, 1)].color : new(0);
            Vector3T<int> filter = new(0) {
                X = adjs.Get(0) - adjs.Get(3),
                Y = adjs.Get(1) - adjs.Get(4),
                Z = adjs.Get(2) - adjs.Get(5)
            };

            var bestnb = nb1.Max(nb2).Max(nb3).Max(nb4).Max(nb5).Max(nb6);
            emit *= bestnb;
            float emax = emit.Max();
            if (0.05f <= emax && top[0] * 0.7f <= emax) {
                top[0] = Math.Max(top[0], emax);
                orders.Add(new(xyz, emit, filter));
            }
        });
        orders = orders.OrderBy((o) => o.Emit.Max()).ToList();
        int it = 0;
        foreach (var order in orders) {
            if (8 <= it) { break; }
            nextsources.Enqueue(order);
            changemap[order.Source] = new(0);
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

            var dist = xyz - source;

            var adjs = world.Adjacency[wind, cind];
            var l1 = adjs[0] ? GetLambert(dist, new(1, 0, 0), filter) : 0;
            var l2 = adjs[1] ? GetLambert(dist, new(0, 1, 0), filter) : 0;
            var l3 = adjs[2] ? GetLambert(dist, new(0, 0, 1), filter) : 0;
            var l4 = adjs[3] ? GetLambert(dist, new(-1, 0, 0), filter) : 0;
            var l5 = adjs[4] ? GetLambert(dist, new(0, -1, 0), filter) : 0;
            var l6 = adjs[5] ? GetLambert(dist, new(0, 0, -1), filter) : 0;
            //float avglambert = (l1 + l2 + l3 + l4 + l5 + l6) / adjs.Sum();
            float bestlambert = Math.Max(Math.Max(Mathf.Max(l1, l2), Mathf.Max(l3, l4)), Mathf.Max(l5, l6));

            float lval = lmap[wind, cind];
            //lval = (lval < 0.5) ? 0 : 1;
            //lval /= MathF.Sqrt(dist.Square().Sum());
            lval /= (dist - filter).Square().Sum();
            var lcol = lval * emit * 0.6f;
            lcol *= bestlambert;
            cmap[wind, cind] = lcol;
            if (cmap[wind, cind] != lcol) {
                GD.Print("ApplyLightmap failed to apply");
            }
            if (lmap[wind, cind] != 0 && (lcol == 0)) {
                GD.Print("ApplyLightmap went wrong in light effects");
            }
        });
    }

    public static float GetLambert(Vector3T<int> tdist, Vector3T<int> tnormal, Vector3T<int> tfilter) {
        //if (!world.Opacity[xyz + tnormal]) { return 0; }

        if (!(tfilter == tnormal)) {
            tdist += tnormal.Do(tfilter, (n, f) => (n == f) ? n : (n - f));
        }

        Vector3 dist = new(tdist.X, tdist.Y, tdist.Z);
        Vector3 normal = new(tnormal.X, tnormal.Y, tnormal.Z);

        dist = dist.Normalized();
        var mult = dist * normal;
        //Ax* Bx +Ay * By + Az * Bz
        return Math.Max(mult.X + mult.Y + mult.Z, 0);
        //return Math.Max(Mathf.Cos(dist.AngleTo(normal)), 0);
    }

    public void MergeColormap(WorldDataVec3 maincomap, WorldDataVec3 changemap, WorldDataVec3 comap) {
        maincomap.ForAll((xyz) => {
            world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var coval = comap[wind, cind];
            var val = changemap[xyz] + coval;
            maincomap[xyz] = val;
            changemap[xyz] = val;
            //maincomap[wind, cind] = val;
            //changemap[wind, cind] = val;

            if (val != changemap[xyz]) {
                GD.Print("mergeColormap failed to merge");
            }
        });
    }

    /*public void ApplyColormap(WorldData<Vector3T<float>> cmap) {
        world.ForAll((xyz) => {
            world.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (!world.Opacity[wind, cind]) {
                //var vox = world[wind, cind];
                //vox.color = cmap[wind, cind];
                world[wind, cind].color = cmap[wind, cind];
            }
        });
    }*/

    private long mainWorkerId;

    public void WaitEnd() {
        Enabled = false;
        WorkerThreadPool.WaitForTaskCompletion(mainWorkerId);
    }
}

public record class LightUpdateResult(WorldDataVec3 Cmap);
public record class LightUpdateMergeRequest(WorldDataVec3 Cmap);
public record class LightUpdateOrder(Vector3T<int> Source, Vector3T<float> Emit, Vector3T<int> Filter);

