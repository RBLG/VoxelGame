using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using voxelgame.scripts;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

using Ivec3 = Vector3T<int>;
using Vec3 = Vector3T<float>;
using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;
using WorldDataFloat = FastWorldData<WorldSettings1, float>;
public class MidLightEngine {
    private static readonly int[] signs = new int[] { 1, -1 };
    private static readonly Ivec3[] axises = new Ivec3[] { new(1, 0, 0), new(0, 1, 0), new(0, 0, 1) };
    private static readonly Ivec3[][] combos = new Ivec3[][] {
        new Ivec3[]{axises[0],axises[1],axises[2]}, new Ivec3[]{axises[0],axises[2],axises[1]},
        new Ivec3[]{axises[1],axises[0],axises[2]}, new Ivec3[]{axises[1],axises[2],axises[0]},
        new Ivec3[]{axises[2],axises[0],axises[1]}, new Ivec3[]{axises[2],axises[1],axises[0]},
    };


    private static readonly IWorldSettings settings = new WorldSettings1();
    private readonly World world;
    private readonly VoxelEngine engine;

    private long mainWorkerId;
    public bool Enabled { get; set; } = true;

    public MidLightEngine(World nworld, VoxelEngine nengine) {
        world = nworld;
        engine = nengine;
    }



    public WorldDataVec3 ComputeVisibilityMap(Ivec3 source) {
        WorldDataVec3 vmap = WorldDataVec3.UnsafeNew();
        foreach (Ivec3[] combo in combos) {
            foreach (int s1 in signs) {
                foreach (int s2 in signs) {
                    foreach (int s3 in signs) { //48 variations
                        IterateInCone(vmap, source, combo[0] * s1, combo[1] * s2, combo[2] * s3);
                    }
                }
            }
        }
        return vmap;
    }

    public void IterateInCone(WorldDataVec3 vmap, Ivec3 source, Ivec3 v1, Ivec3 v2, Ivec3 v3) {
        vmap[source] = new(1, 0, 0);

        Ivec3 mins = settings.TotalMins - source;
        Ivec3 maxs = settings.TotalMaxs - source;

        int bound1 = v1.Do(mins, maxs, (v, min, max) => (v < 0) ? -min : max).Pick(v1);
        int bound2 = v2.Do(mins, maxs, (v, min, max) => (v < 0) ? -min : max).Pick(v2);
        int bound3 = v3.Do(mins, maxs, (v, min, max) => (v < 0) ? -min : max).Pick(v3);

        for (int it1 = 1; it1 <= bound1; it1++) { //start at 1 to skip source
            Ivec3 vit1 = v1 * it1;
            for (int it2 = 0; it2 <= bound2 && it2 <= it1; it2++) {
                Ivec3 vit2 = v2 * it2;
                for (int it3 = 0; it3 <= bound3 && it3 <= it2; it3++) {
                    Ivec3 sdist = vit1 + vit2 + (v3 * it3);
                    Ivec3 xyz = source + sdist;
                    if (xyz < settings.TotalMins || settings.TotalMaxs < xyz) {
                        GD.Print($"xyz oob: {xyz.ToShortString()}");
                    }

                    if (world.Occupancy[xyz]) { continue; }
                    //biases
                    var b3 = it3;
                    var b2 = it2 - it3;
                    var b1 = it1 - Math.Max(it2, it3);

                    //neigbors
                    var nb1 = vmap[xyz - v1].X * b1;
                    var nb2 = (it2 == 0) ? 0 : vmap[xyz - v1 - v2].X * b2;
                    var nb3 = (it3 == 0) ? 0 : vmap[xyz - v1 - v2 - v3].X * b3;

                    var nval = (nb1 + nb2 + nb3) / (b1 + b2 + b3);
                    vmap[xyz] = new(nval);
                }
            }
        }
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
        nextsources.Enqueue(new(new(+11, +62, 10), RandomColor() * 330, new(0)));
        nextsources.Enqueue(new(new(+73, -12, 13), RandomColor() * 500, new(0)));
        nextsources.Enqueue(new(new(-50, -38, -3), RandomColor() * 300, new(0)));

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
        float[] top = new float[] { 0.15f };
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

                Ivec3 filter = new(0) {
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

    public void ComputeOneLightmap(Ivec3 source, Vec3 emit, Ivec3 filter) {
        WorldDataVec3 vmap = ComputeVisibilityMap(source);
        WorldDataVec3 cmap = ComputeLightmap(vmap, source, emit, filter);

        mergequeue.Enqueue(new(cmap));
    }

    public WorldDataVec3 ComputeLightmap(WorldDataVec3 vmap, Ivec3 source, Vec3 emit, Ivec3 filter) {
        WorldDataVec3 lmap = vmap;
        lmap.ForAll((xyz) => {// light_color * light_intensity * GBV_value * lambert / distance^2
            if (xyz == source) { return; }
            lmap.DeconstructPosToIndex(xyz, out var wind, out var cind);
            if (world.Occupancy[wind, cind]) { return; }

            float lval = vmap[wind, cind].X;
            vmap[wind, cind] = new(0);
            //lval = Mathf.Clamp(lval * 10 - 5, 0, 1);
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

    public static float GetLambert(Ivec3 tdist, Ivec3 tnormal, Ivec3 tfilter) {
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

    public static Vec3 RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());

    private readonly ConcurrentQueue<LightUpdateOrder> nextsources = new();
    private readonly ConcurrentQueue<LightUpdateMergeRequest> mergequeue = new();

    public record class LightUpdateResult(WorldDataVec3 Cmap);
    public record class LightUpdateMergeRequest(WorldDataVec3 Cmap);
    public record class LightUpdateOrder(Ivec3 Source, Vec3 Emit, Ivec3 Filter);

}

