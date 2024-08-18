using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using VoxelGame.scripts.common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VoxelGame.scripts.content;
public class World {
    public readonly CenteredArray3D<Chunk> chunks;
    public readonly WorldBinaryData Opacity;
    public readonly WorldData<Bool8Array> Adjacency;

    public Vector3T<uint> Size { get; }
    public Vector3T<long> Maxs { get; }
    public Vector3T<long> Mins { get; }

    public World(Vector3T<uint> size, Vector3T<long> center) {
        chunks = new(size, center);
        Opacity = new(this);
        Adjacency = new(this, () => new());
        Size = chunks.Size * Chunk.Size;
        Maxs = ((chunks.Size.Do((val) => (long)val) - chunks.Center) * Chunk.LSize) - 1;
        Mins = -chunks.Center * Chunk.LSize; ;
    }

    public void Generate(WorldGenerator1 generator) {
        chunks.ForAll((xyz) => {
            chunks[xyz] = generator.GenerateChunk(xyz, out var OpacityChunk);
            Opacity.chunks[xyz] = OpacityChunk;
        });
        //generator.InsertFeatures(this);
        ForAll((xyz) => {
            bool a1 = (xyz.X != Maxs.X) && Opacity[xyz + (1, 0, 0)];
            bool a2 = (xyz.Y != Maxs.Y) && Opacity[xyz + (0, 1, 0)];
            bool a3 = (xyz.Z != Maxs.Z) && Opacity[xyz + (0, 0, 1)];
            bool a4 = (xyz.X != Mins.X) && Opacity[xyz - (1, 0, 0)];
            bool a5 = (xyz.Y != Mins.Y) && Opacity[xyz - (0, 1, 0)];
            bool a6 = (xyz.Z != Mins.Z) && Opacity[xyz - (0, 0, 1)];
            Adjacency[xyz].Set(a1, a2, a3, a4, a5, a6);
        });
    }

    public Voxel this[Vector3T<long> xyz] {
        get {
            DeconstructPos(xyz, out var wpos, out var cpos);
            return chunks[wpos].voxels[cpos];
        }
        set {
            DeconstructPos(xyz, out var wpos, out var cpos);
            chunks[wpos].voxels[cpos] = value;
        }
    }

    public Voxel this[uint wind, uint cind] {
        get => chunks[wind].voxels[cind];
        set => chunks[wind].voxels[cind] = value;
    }

    public static void DeconstructPos(Vector3T<long> pos, out Vector3T<long> wpos, out Vector3T<long> cpos) {
        if (Chunk.Size == 4) {
            cpos = pos.Do((val) => val & 0b11);
            wpos = pos.Do(cpos, (val, cval) => (val - cval) >> 2);
            return;
        } else {
            cpos = pos.Modulo(Chunk.LSize);
            wpos = (pos - cpos) / Chunk.LSize;
            return;
        }
    }

    public void DeconstructPosToIndex(Vector3T<long> pos, out uint wind, out uint cind) {
        DeconstructPos(pos, out var wpos, out var cpos);
        wind = chunks.GetIndexFromXyz(wpos.X, wpos.Y, wpos.Z);
        cind = BinaryArray3D<object>.GetIndexFromXyz((uint)cpos.X, (uint)cpos.Y, (uint)cpos.Z, Chunk.BitSize.X, Chunk.BitSize.Y);
    }

    public void ForAll(Action<Vector3T<long>> action) {
        for (long itx = Mins.X; itx <= Maxs.X; itx++) {
            for (long ity = Mins.Y; ity <= Maxs.Y; ity++) {
                for (long itz = Mins.Z; itz <= Maxs.Z; itz++) {
                    action(new(itx, ity, itz));
                }
            }
        }
    }
}

public sealed class Chunk {
    public static readonly Vector3T<int> BitSize = new(2);
    public static readonly Vector3T<uint> Size = new(4);
    public static readonly Vector3T<long> LSize = new(4);
    public readonly BinaryArray3D<Voxel> voxels;
    public Chunk() {
        voxels = new(new(2));
        voxels.Initialize();
    }
}

public record class Voxel {
    public Vector3T<float> color;
    //public bool Opaque = false;
    public Vector2T<byte> uv = new(3, 3);
    public Vector3T<byte> emit; //just a float?
}

public class WorldData<DATA> {
    public readonly CenteredArray3D<BinaryArray3D<DATA>> chunks;
    private readonly World world;

    public WorldData(World nworld, Func<DATA> filler) {
        world = nworld;
        chunks = new(world.chunks.Size, world.chunks.Center);
        chunks.ForAll((xyz) => {
            BinaryArray3D<DATA> data = new(new(2));
            data.ForAll((xyz2) => data[xyz2] = filler());
            chunks[xyz] = data;
        });
    }

    public DATA this[Vector3T<long> xyz] {
        get {
            World.DeconstructPos(xyz, out var wpos, out var cpos);
            return chunks[wpos][cpos];
        }
        set {
            World.DeconstructPos(xyz, out var wpos, out var cpos);
            chunks[wpos][cpos] = value;
        }
    }

    public DATA this[Vector3T<long> wxyz, Vector3T<long> cxyz] {
        get => chunks[wxyz][cxyz];
        set => chunks[wxyz][cxyz] = value;
    }

    public DATA this[uint wind, uint cind] {
        get => chunks[wind][cind];
        set => chunks[wind][cind] = value;
    }

    public void ForAll(Action<Vector3T<long>> action) => world.ForAll(action);

}

public class WorldBinaryData {
    public readonly CenteredArray3D<BooleanArray3D> chunks;
    private readonly World world;
    public WorldBinaryData(World nworld) {
        world = nworld;
        chunks = new(world.chunks.Size, world.chunks.Center);
    }

    public bool this[Vector3T<long> xyz] {
        get {
            World.DeconstructPos(xyz, out var wpos, out var cpos);
            return chunks[wpos][cpos];
        }
        set {
            World.DeconstructPos(xyz, out var wpos, out var cpos);
            chunks[wpos][cpos] = value;
        }
    }

    public bool this[Vector3T<long> wxyz, Vector3T<long> cxyz] {
        get => chunks[wxyz][cxyz];
        set => chunks[wxyz][cxyz] = value;
    }

    public bool this[uint wind, uint cind] {
        get => chunks[wind][cind];
        set => chunks[wind][cind] = value;
    }

    public void ForAll(Action<Vector3T<long>> action) => world.ForAll(action);
}

