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
public class MidPlusLightEngine {
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
    private readonly WorldDataVec3 currentmap;
    private readonly WorldDataVec3 updatemap;

    public MidPlusLightEngine(World nworld, VoxelEngine nengine) {
        world = nworld;
        engine = nengine;
        currentmap = new((wind, cind) => world.Voxels[wind, cind].color);
        updatemap = new((wind, cind) => currentmap[wind, cind]);
    }


    public void AsyncComputeVisibilityMap(Ivec3 source, Vec3 emit) {
        WorldDataFloat vmap = WorldDataFloat.UnsafeNew();

        var adjs = world.Adjacency[source];
        Ivec3 filter = new(0) {
            X = adjs.Get(0) - adjs.Get(3),
            Y = adjs.Get(1) - adjs.Get(4),
            Z = adjs.Get(2) - adjs.Get(5)
        };

        //List<long> ids = new(48);
        foreach (Ivec3[] combo in combos) {
            foreach (int s1 in signs) {
                foreach (int s2 in signs) {
                    foreach (int s3 in signs) { //48 variations
                        Ivec3 v1 = combo[0] * s1;
                        Ivec3 v2 = combo[1] * s2;
                        Ivec3 v3 = combo[2] * s3;
                        IterateInCone(vmap, source, emit, filter, v1, v2, v3);
                        //long id = WorkerThreadPool.AddTask(Callable.From(() => IterateInCone(vmap, source, emit, filter, v1, v2, v3)));
                        //ids.Add(id);
                    }
                }
            }
        }
        /*GD.Print($"waiting on {ids.Count} cones");
        foreach (var id in ids) {
            WorkerThreadPool.WaitForTaskCompletion(id);
        }*/
        //return vmap;
    }

    public void IterateInCone(WorldDataFloat vmap, Ivec3 source, Vec3 emit, Ivec3 filter, Ivec3 v1, Ivec3 v2, Ivec3 v3) {
        vmap[source] = 1;

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
                    currentmap.DeconstructPosToIndex(xyz, out var wind, out var cind);

                    if (world.Occupancy[wind, cind]) { continue; }
                    //biases
                    var b3 = it3;
                    var b2 = it2 - it3;
                    var b1 = it1 - Math.Max(it2, it3);

                    //neigbors
                    var nb1 = vmap[xyz - v1] * b1;
                    var nb2 = (it2 == 0) ? 0 : vmap[xyz - v1 - v2] * b2;
                    var nb3 = (it3 == 0) ? 0 : vmap[xyz - v1 - v2 - v3] * b3;

                    var visi = (nb1 + nb2 + nb3) / (b1 + b2 + b3);
                    vmap[wind, cind] = visi;

                    if (visi == 0) { continue; }
                    
                    //get light value from visibility
                    var adjs = world.Adjacency[wind, cind]; //TODO also do source (filter) lambert
                    if (adjs.IsEmpty()) { continue; }
                    var bestlambert = GetBestLambert(adjs, sdist, filter);

                    var lcol = visi * emit * 1.0f * bestlambert / (sdist.Square().Sum() + 1);
                    currentmap[wind, cind] += lcol;
                }
            }
        }
    }

    public static float GetBestLambert(Bool8Pack adjs, Ivec3 dist, Ivec3 filter) {
        var l1 = adjs[0] ? GetLambert(dist, new(1, 0, 0), filter) : 0;
        var l2 = adjs[1] ? GetLambert(dist, new(0, 1, 0), filter) : 0;
        var l3 = adjs[2] ? GetLambert(dist, new(0, 0, 1), filter) : 0;
        var l4 = adjs[3] ? GetLambert(dist, new(-1, 0, 0), filter) : 0;
        var l5 = adjs[4] ? GetLambert(dist, new(0, -1, 0), filter) : 0;
        var l6 = adjs[5] ? GetLambert(dist, new(0, 0, -1), filter) : 0;
        //float avglambert = (l1 + l2 + l3 + l4 + l5 + l6) / adjs.Sum(); //TODO remove adjs that arent facing the ray
        float bestlambert = Mathf.Max(Mathf.Max(Mathf.Max(l1, l2), Mathf.Max(l3, l4)), Mathf.Max(l5, l6));
        return 1;
    }


    public static float GetLambert(Ivec3 tdist, Ivec3 tnormal, Ivec3 tfilter) {
        if (tfilter == tnormal) { return 0; }
        tdist += tnormal;

        Vector3 normal = tnormal.ToVector3();
        Vector3 dist = tdist.ToVector3();
        dist = dist.Normalized();

        //Ax* Bx +Ay * By + Az * Bz
        var mult = dist * normal;
        return Mathf.Max(mult.X + mult.Y + mult.Z, 0);
    }

    public void AsyncStartLighting() {
        mainWorkerId = WorkerThreadPool.AddTask(Callable.From(ComputeLighting));
    }

    public void ComputeLighting() {
        if (!Enabled) { return; }

        currentmap[new(+02, +02, -9)] = RandomColor() * 680;
        currentmap[new(+11, +62, 10)] = RandomColor() * 330;
        currentmap[new(+73, -12, 13)] = RandomColor() * 500;
        currentmap[new(-50, -38, -3)] = RandomColor() * 300;
        world.Occupancy[new(+02, +02, -9)] = true;
        world.Occupancy[new(+11, +62, 10)] = true;
        world.Occupancy[new(+73, -12, 13)] = true;
        world.Occupancy[new(-50, -38, -3)] = true;

        GD.Print("starting the light computation");
        while (true) {
            GD.Print("finding next source");
            if (GetNextTopSource(out var nextsource, out var nextemit)) {
                break;
            }
            GD.Print("computing lightmap");
            AsyncComputeVisibilityMap(nextsource, nextemit);

            if (!Enabled) { return; }
            GD.Print("preparing layers update");
            engine.PrepareColorLayers(currentmap);
        }
        GD.Print("no more sources");
    }

    public void Stop() {
        GD.Print("stopping light updates");
        Enabled = false;
        WorkerThreadPool.WaitForTaskCompletion(mainWorkerId);
    }

    public bool GetNextTopSource(out Ivec3 nsource, out Vec3 emit) {
        float[] top = new float[] { 0.15f };
        Ivec3[] nextsource = new Ivec3[] { new() };

        updatemap.ForAll((xyz) => {
            world.Voxels.DeconstructPosToIndex(xyz, out var wind, out var cind);
            var emit = currentmap[wind, cind] - updatemap[wind, cind];
            if (emit == 0) { return; }

            var adjs = world.Adjacency[wind, cind];
            if (!world.Occupancy[wind, cind]) {
                if (adjs.IsEmpty()) { return; }
                var nb1 = adjs[0] ? world.Voxels[xyz + (1, 0, 0)].color : new(0);
                var nb2 = adjs[1] ? world.Voxels[xyz + (0, 1, 0)].color : new(0);
                var nb3 = adjs[2] ? world.Voxels[xyz + (0, 0, 1)].color : new(0);
                var nb4 = adjs[3] ? world.Voxels[xyz - (1, 0, 0)].color : new(0);
                var nb5 = adjs[4] ? world.Voxels[xyz - (0, 1, 0)].color : new(0);
                var nb6 = adjs[5] ? world.Voxels[xyz - (0, 0, 1)].color : new(0);
                var bestnb = nb1.Max(nb2).Max(nb3).Max(nb4).Max(nb5).Max(nb6);
                emit *= bestnb;
            }

            float emax = emit.Max();
            if (top[0] <= emax) {
                top[0] = Math.Max(top[0], emax);
                nextsource[0] = xyz;
            }
        });
        GD.Print($"next source emax={top[0]}");
        nsource = nextsource[0];
        emit = currentmap[nsource] - updatemap[nsource];
        updatemap[nsource] = currentmap[nsource];
        return top[0] < 0.1f;
    }

    public static Vec3 RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());
}

