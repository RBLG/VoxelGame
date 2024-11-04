using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using voxelgame.scripts;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

using Ivec3 = Vector3T<int>;
using Vec3 = Vector3T<float>;
using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;
using WorldDataFloat = FastWorldData<WorldSettings1, float>;
using static System.Formats.Asn1.AsnWriter;

public class MidPlusLightEngine {
    private static readonly int[] SIGNS = new int[] { 1, -1 };
    // positive axis
    private static readonly Ivec3 X = new(1, 0, 0);
    private static readonly Ivec3 Y = new(0, 1, 0);
    private static readonly Ivec3 Z = new(0, 0, 1);
    // unsigned cones
    private static readonly UCone XYZ = new(X, Y, Z, 1, 0); // TODO move to an enum?
    private static readonly UCone XZY = new(X, Z, Y, 0, 1); // TODO add a (1,2,3) sig for order?
    private static readonly UCone YXZ = new(Y, X, Z, 0, 0);
    private static readonly UCone YZX = new(Y, Z, X, 0, 1);
    private static readonly UCone ZXY = new(Z, X, Y, 1, 1);
    private static readonly UCone ZYX = new(Z, Y, X, 1, 0);
    private static readonly UCone[] UCONES = new UCone[] { XYZ, XZY, YXZ, YZX, ZXY, ZYX, };
    // all 48 signed cones
    private static readonly Cone[] CONES = GenCones();

    private static Cone[] GenCones() {
        Cone[] ncones = new Cone[48];
        int index = 0;
        foreach (UCone ucone in UCONES) {
            foreach (int s1 in SIGNS) {
                foreach (int s2 in SIGNS) {
                    foreach (int s3 in SIGNS) { // 48 variations
                        Ivec3 v1 = ucone.axis1 * s1;
                        Ivec3 v2 = ucone.axis2 * s2;
                        Ivec3 v3 = ucone.axis3 * s3;
                        ncones[index] = new Cone(v1, v2, v3, ucone.edge1, ucone.edge2, 0 < s2, 0 < s3);
                        index++;
                    }
                }
            }
        }
        return ncones;
    }



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


    public void AsyncTraceAllCones(Ivec3 source, Vec3 emit) {
        var adjs = world.Adjacency[source];
        Ivec3 filter = new(0) {
            X = adjs.Get(0) - adjs.Get(3),
            Y = adjs.Get(1) - adjs.Get(4),
            Z = adjs.Get(2) - adjs.Get(5)
        };
        if (world.Occupancy[source]) { filter = new(0); }

        using CountdownEvent cdevent = new(48);
        foreach (Cone cone in CONES) {// 48 times
            ThreadPool.QueueUserWorkItem(delegate {
                TraceCone(source, emit, filter, cone);
                cdevent.Signal();
            });
        }
        cdevent.Wait();
    }

    public void TraceCone(Ivec3 source, Vec3 emit, Ivec3 filter, Cone cone) {
        Ivec3 mins = settings.TotalMins - source; //world negative bound (included)
        Ivec3 maxs = settings.TotalMaxs - source; //world positive bound (included)

        //select mins or max based on if vn is positive or negative, then pick the one corresponding to the right axis
        int bound1 = ((cone.axis1 <= 0) ? -mins : maxs).Pick(cone.axis1);
        int bound2 = ((cone.axis2 <= 0) ? -mins : maxs).Pick(cone.axis2);
        int bound3 = ((cone.axis3 <= 0) ? -mins : maxs).Pick(cone.axis3);

        //store the visibility values
        float[,] vbuffer = new float[Math.Min(bound1, bound2) + 1 + 1, Math.Min(bound1, bound3) + 1 + 1];
        vbuffer[0, 0] = 1; //the source

        for (int it1 = 0; it1 <= bound1; it1++) { //start at 1 to skip source
            Ivec3 vit1 = cone.axis1 * it1;
            float it1inv = 1f / (it1 + 1);
            bool planevisi = false;
            for (int it2 = Math.Min(bound2, it1); 0 <= it2; it2--) {// start from the end to handle neigbors replacement easily
                Ivec3 vit2 = cone.axis2 * it2 + vit1;
                for (int it3 = Math.Min(bound3, it2); 0 <= it3; it3--) { //same than it2
                    Ivec3 sdist = cone.axis3 * it3 + vit2; //signed distance
                    Ivec3 xyz = source + sdist; //world position
                    (int wind, int cind) = WorldDataVec3.StaticDeconstructPosToIndex(xyz); //optimization shenanigans,tldr wind,cind is xyz

                    float visi = vbuffer[it2, it3];
                    if (visi == 0) {
                        vbuffer[it2, it3] = 0;
                        continue;
                    }
                    planevisi = true;

                    if (world.Occupancy[wind, cind] && it1 != 0) {
                        vbuffer[it2, it3] = 0;
                        continue;
                    }

                    //weights
                    int w1 = it1 + 1 - it2;
                    int w2 = it2 + 1 - it3;
                    int w3 = it3 + 1;

                    vbuffer[it2, it3] = visi * w1 * it1inv;
                    vbuffer[it2 + 1, it3] += visi * w2 * it1inv;
                    vbuffer[it2 + 1, it3 + 1] += visi * w3 * it1inv;


                    // end visibility computation, light effects start here

                    if (it1 == 0) { continue; }

                    //avoid duplicated edges
                    if ((it1 == it2 && cone.edge1) || (it2 == it3 && cone.edge2) || (it2 == 0 && cone.qedge2) || (it3 == 0 && cone.qedge3)) {
                        continue;
                    }

                    //get light exposure value and add it to the world
                    //var adjs = world.Adjacency[wind, cind];
                    //if (adjs.IsEmpty()) { continue; }
                    float bestlambert = 1;// GetBestLambert(adjs, sdist, filter);
                    currentmap[wind, cind] += visi * emit * bestlambert / (sdist.Square().Sum() + 1);
                }
            }
            if (!planevisi) { break; }
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
        return bestlambert;
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
        currentmap[new(+73, +12, 13)] = RandomColor() * 500;
        currentmap[new(-50, -18, -3)] = RandomColor() * 300;
        world.Occupancy[new(+02, +02, -9)] = true;
        world.Occupancy[new(+11, +62, 10)] = true;
        world.Occupancy[new(+73, +12, 13)] = true;
        world.Occupancy[new(-50, -18, -3)] = true;

        Stopwatch sw = new();
        int count = 0;

        GD.Print("starting the light computation");
        while (true) {
            GD.Print("finding next sources");
            List<UpdateRequest> requests = GetNextTopSource();
            if (requests.Count == 0) { break; }
            sw.Start();
            foreach (var request in requests) {
                //GD.Print("computing lighting from a source");
                AsyncTraceAllCones(request.Source, request.Emit);
                count++;
            }
            sw.Stop();
            if (2000 <= count) { break; }

            if (!Enabled) { return; }
            GD.Print("preparing layers update");
            WorkerThreadPool.AddTask(Callable.From(() => engine.PrepareColorLayers(currentmap)));
            //engine.PrepareColorLayers(currentmap);
        }
        GD.Print("no more sources");
        float millis = sw.ElapsedMilliseconds;
        float avg = millis / count;
        GD.Print($"{count} source done over {settings.TotalSize.X}*{settings.TotalSize.Y}*{settings.TotalSize.Z} scene in {millis}ms (average {avg:0.000}ms)");
    }

    public void Stop() {
        GD.Print("stopping light updates");
        Enabled = false;
        WorkerThreadPool.WaitForTaskCompletion(mainWorkerId);
    }

    public List<UpdateRequest> GetNextTopSource() {
        List<UpdateRequest> norders = new();
        currentmap.ForAll((xyz) => {
            currentmap.DeconstructPosToIndex(xyz, out var wind, out var cind);
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
            if (0.1 <= emax) {
                updatemap[wind, cind] = currentmap[wind, cind];
                norders.Add(new(xyz, emit));
            }
        });
        return norders.OrderBy((o) => -o.Emit.Max()).Take(30).ToList();
    }

    public record class UpdateRequest(Ivec3 Source, Vec3 Emit);

    public static Vec3 RandomColor() => new(GD.Randf(), GD.Randf(), GD.Randf());
}



public record struct Cone(// signed iteration cone
        Ivec3 axis1, Ivec3 axis2, Ivec3 axis3, //
        bool edge1, bool edge2, // diagonal edges priorities
        bool qedge2, bool qedge3 // quadrant edges priorities
) { }

public record struct UCone(Ivec3 axis1, Ivec3 axis2, Ivec3 axis3, bool edge1, bool edge2) { // unsigned cone

    public UCone(Ivec3 naxis1, Ivec3 naxis2, Ivec3 naxis3, int nedge1, int nedge2) : this(naxis1, naxis2, naxis3, nedge1 == 0, nedge2 == 0) { }

}

