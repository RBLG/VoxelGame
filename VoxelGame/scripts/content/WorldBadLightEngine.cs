using Godot;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;
public class WorldBadLightEngine {
    private readonly World world;

    public WorldBadLightEngine(World nworld) {
        world = nworld;
    }

    private static readonly Vector3T<long>[] dirs = new Vector3T<long>[]{
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

    private static readonly Vector3T<long>[] faceNormals = new Vector3T<long>[] {
        new( 1, 0, 0), new( 0, 1, 0), new( 0, 0, 1),
        new(-1, 0, 0), new( 0,-1, 0), new( 0, 0,-1),
    };

    public void AsyncDoLighting() {
        WorkerThreadPool.AddTask(Callable.From(DoLighting));
        //Parallel.Invoke(DoLighting);
    }

    private readonly ConcurrentQueue<LightUpdateOrder> nextsources = new();
    private readonly ConcurrentQueue<LightUpdateMergeRequest> mergequeue = new();
    private readonly ConcurrentQueue<LightUpdateResult> changequeue = new();



    public static Vector3T<float> RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());

    public void DoLighting() {
        WorldData<Vector3T<float>> maincmap = new(world, () => new(0f));
        WorldData<Vector3T<float>> emitmap = new(world, () => new(0f));
        emitmap[new(+02, +02, -9)] = maincmap[new(+02, +02, -9)] = RandomColor() * 10;
        emitmap[new(+11, +32, 10)] = maincmap[new(+11, +32, 10)] = RandomColor() * 10;
        emitmap[new(+43, -12, 13)] = maincmap[new(+43, -12, 13)] = RandomColor() * 10;
        emitmap[new(-30, +08, -3)] = maincmap[new(-30, +08, -3)] = RandomColor() * 10;
        //nextsources.Enqueue(new(new(+02, +02, -9), maincmap[new(+02, +02, -9)], new(0))); ;
        //nextsources.Enqueue(new(new(+11, +32, 10), maincmap[new(+11, +32, 10)], new(0)));
        //nextsources.Enqueue(new(new(+43, -12, 13), maincmap[new(+43, -12, 13)], new(0)));
        //nextsources.Enqueue(new(new(-30, +08, -3), maincmap[new(-30, +08, -3)], new(0)));

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
            if (ApplyMergeQueue(maincmap, emitmap)) {
                changequeue.Enqueue(new(maincmap));
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



    public bool ApplyMergeQueue(WorldData<Vector3T<float>> mcmap, WorldData<Vector3T<float>> changemap) {
        bool updated = false;
        while (mergequeue.TryDequeue(out LightUpdateMergeRequest? rslt)) {
            MergeColormap(mcmap, changemap, rslt.Cmap);
            updated = true;
        }
        return updated;
    }


    public bool ApplyLatestResults() {
        bool updated = false;
        while (changequeue.TryDequeue(out LightUpdateResult? rslt)) {
            ApplyColormap(rslt.Cmap);
            updated = true;
        }
        return updated;
    }

    public void QueueSignificantSources(WorldData<Vector3T<float>> changemap) {

        float[] top = new float[] { 0.05f };
        List<LightUpdateOrder> orders = new();


        changemap.ForAll((xyz) => {
            world.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var emit = changemap[wind, cind];

            var adjs = world.Adjacency[wind, cind];
            var nb1 = adjs[0] ? world[xyz + (1, 0, 0)].color : new(0);
            var nb2 = adjs[1] ? world[xyz + (0, 1, 0)].color : new(0);
            var nb3 = adjs[2] ? world[xyz + (0, 0, 1)].color : new(0);
            var nb4 = adjs[3] ? world[xyz - (1, 0, 0)].color : new(0);
            var nb5 = adjs[4] ? world[xyz - (0, 1, 0)].color : new(0);
            var nb6 = adjs[5] ? world[xyz - (0, 0, 1)].color : new(0);
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
            if (16 <= it) { break; }
            nextsources.Enqueue(order);
            changemap[order.Source] = new(0);
            it++;
            //GD.Print($"new order filter: {order.Filter.X}:{order.Filter.Y}:{order.Filter.Z}");
        }
    }

    public void DoLightingOnce(Vector3T<long> source, Vector3T<float> emit, Vector3T<int> filter) {
        WorldData<float> lmap = new(world, () => 0f);


        DoSourceLighting(source, lmap, filter);
        WorldData<Vector3T<float>> cmap = new(world, () => new(0f));
        ApplyLightmap(cmap, lmap, source, emit, filter.Do((v) => (long)v));

        mergequeue.Enqueue(new(cmap));
    }

    public void DoSourceLighting(Vector3T<long> pos, WorldData<float> lmap, Vector3T<int> filter) {//vec3 dir, decayrate
        var mins = world.Mins;
        var maxs = world.Maxs;

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
                //continue;
            }

            var adir = dir.Abs();
            var spos = pos + dir;
            var maxs2 = dir.Do(maxs, spos, (d, max, spo) => (d == 0) ? spo : max); //(maxs * adir).Max(spos);
            var mins2 = dir.Do(mins, spos, (d, min, spo) => (d == 0) ? spo : min);

            var incr = dir.Do((val) => (0 <= val) ? 1 : -1);
            //GD.Print("starting new quadrant iteration");
            for (long itx = spos.X; IsInBoundII(itx, mins2.X, maxs2.X); itx += incr.X) {
                for (long ity = spos.Y; IsInBoundII(ity, mins2.Y, maxs2.Y); ity += incr.Y) {
                    for (long itz = spos.Z; IsInBoundII(itz, mins2.Z, maxs2.Z); itz += incr.Z) {
                        Vector3T<long> xyz = new(itx, ity, itz);
                        world.DeconstructPosToIndex(xyz, out var wind, out var cind);
                        if (world.Opacity[wind, cind]) {
                            continue;
                        }
                        var dist = pos.DistanceTo(xyz);
                        var vx = lmap[new(itx - dir.X, ity, itz)];
                        var vy = lmap[new(itx, ity - dir.Y, itz)];
                        var vz = lmap[new(itx, ity, itz - dir.Z)];
                        vx *= dist.X;
                        vy *= dist.Y;
                        vz *= dist.Z;

                        lmap[wind, cind] = (vx + vy + vz) / dist.Sum();
                    }
                }
            }

        }

    }

    public static bool IsInBoundII(long pos, long min, long max) {
        return min <= pos && pos <= max;
    }

    public void ApplyLightmap(WorldData<Vector3T<float>> cmap, WorldData<float> lmap, Vector3T<long> source, Vector3T<float> emit, Vector3T<long> filter) {
        //var sadjs = world.Adjacency[source];
        //bool isface=sadjs.Sum()==1;

        cmap.ForAll((xyz) => {// light_color * light_intensity * GBV_value * lambert / distance^2
            if (xyz == source) { return; }
            world.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (world.Opacity[wind, cind]) { return; }

            var dist = xyz - source;

            var adjs = world.Adjacency[wind, cind];
            var l1 = adjs[0] ? GetLambert(dist, new(1, 0, 0), filter) : 0;
            var l2 = adjs[1] ? GetLambert(dist, new(0, 1, 0), filter) : 0;
            var l3 = adjs[2] ? GetLambert(dist, new(0, 0, 1), filter) : 0;
            var l4 = adjs[3] ? GetLambert(dist, new(-1, 0, 0), filter) : 0;
            var l5 = adjs[4] ? GetLambert(dist, new(0, -1, 0), filter) : 0;
            var l6 = adjs[5] ? GetLambert(dist, new(0, 0, -1), filter) : 0;
            float avglambert = (l1 + l2 + l3 + l4 + l5 + l6) / adjs.Sum();
            //var bestlambert = Math.Max(Math.Max(Mathf.Max(l1, l2), Mathf.Max(l3, l4)), Mathf.Max(l5, l6));

            float lval = lmap[wind, cind];
            //lval = (lval < 0.5) ? 0 : 1;
            //lval /= MathF.Sqrt(dist.Square().Sum());
            lval /= (dist - filter).Square().Sum();
            var lcol = lval * emit * 1f;

            cmap[wind, cind] = lcol * avglambert;
        });
    }

    public static float GetLambert(Vector3T<long> tdist, Vector3T<long> tnormal, Vector3T<long> tfilter) {
        //if (!world.Opacity[xyz + tnormal]) { return 0; }

        if (!(tfilter == tnormal)) {
            tdist += tnormal.Do(tfilter, (n, f) => (n == f) ? n : (n - f));
            //tdist += tnormal;
        }

        Vector3 dist = new(tdist.X, tdist.Y, tdist.Z);
        Vector3 normal = new(tnormal.X, tnormal.Y, tnormal.Z);
        //Vector3 filter = new(tfilter.X, tfilter.Y, tfilter.Z);

        //dist += (normal - filter) * 0.5f;

        dist = dist.Normalized();
        var mult = dist * normal;
        return Math.Max(mult.X + mult.Y + mult.Z, 0) + 0.000001f; //Ax* Bx +Ay * By + Az * Bz
                                                                  //return Math.Max(Mathf.Cos(dist.AngleTo(normal)), 0);
    }

    public void MergeColormap(WorldData<Vector3T<float>> maincmap, WorldData<Vector3T<float>> changemap, WorldData<Vector3T<float>> cmap) {
        maincmap.ForAll((xyz) => {
            world.DeconstructPosToIndex(xyz, out var wind, out var cind);
            maincmap[wind, cind] += cmap[wind, cind];
            changemap[wind, cind] += cmap[wind, cind];
        });
    }

    public void ApplyColormap(WorldData<Vector3T<float>> cmap) {
        world.ForAll((xyz) => {
            world.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (!world.Opacity[wind, cind]) {
                //var vox = world[wind, cind];
                //vox.color = cmap[wind, cind];
                world[wind, cind].color = cmap[wind, cind];
            }
        });
    }

}

public record class LightUpdateResult(WorldData<Vector3T<float>> Cmap);
public record class LightUpdateMergeRequest(WorldData<Vector3T<float>> Cmap);
public record class LightUpdateOrder(Vector3T<long> Source, Vector3T<float> Emit, Vector3T<int> Filter);

