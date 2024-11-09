using System;
using VoxelGame.scripts.common.arrays;
using VoxelGame.scripts.common.math;

namespace VoxelGame.scripts.content.worlddata;

using Ivec3 = Vector3T<int>;

public class SparseWorldData<SETTINGS, ARRAY, DATA> where SETTINGS : IWorldSettings, new() where ARRAY : IArray3d<DATA> where DATA : new()
{
    protected static readonly SETTINGS settings = new();

    public CenteredArray3D<ARRAY?> Chunks { get; }

    private readonly Func<ARRAY> initer;
    private readonly ARRAY dummy;
    public SparseWorldData(Func<ARRAY> niniter)
    {
        Chunks = new(settings.GridSize, settings.GridCenter);
        initer = niniter;
        dummy = initer();
    }

    public void DeconstructPosToIndex(Ivec3 pos, out int wind, out int cind)
    {
        WorldData<SETTINGS, ARRAY, DATA>.DeconstructPos(pos, out var wpos, out var cpos);

        wind = Chunks.GetIndexFromXyz(wpos);
        cind = dummy.GetIndexFromXyz(cpos);
    }

    public DATA this[Ivec3 xyz]
    {
        get
        {
            WorldData<SETTINGS, ARRAY, DATA>.DeconstructPos(xyz, out var wpos, out var cpos);
            var chunk = Chunks[wpos];
            return chunk == null ? new() : chunk[cpos];
        }
        set
        {
            WorldData<SETTINGS, ARRAY, DATA>.DeconstructPos(xyz, out var wpos, out var cpos);
            var chunk = Chunks[wpos];
            if (chunk == null)
            {
                Chunks[wpos] = chunk = initer();
                chunk.ForAll((xyz) => chunk[xyz] = new());
            }
            chunk[cpos] = value;
        }
    }
    public DATA this[int wind, int cind]
    {
        get
        {
            var chunk = Chunks[wind];
            return chunk == null ? new() : chunk[cind];
        }

        set
        {
            var chunk = Chunks[wind];
            if (chunk == null)
            {
                Chunks[wind] = chunk = initer();
                chunk.ForAll((xyz) => chunk[xyz] = new());
            }
            chunk[cind] = value;
        }
    }

    public bool IsSparse(Ivec3 xyz)
    {
        WorldData<SETTINGS, ARRAY, DATA>.DeconstructPos(xyz, out var wpos, out var cpos);
        return Chunks[wpos] == null;
    }

    public bool IsSparse(int wind)
    {
        return Chunks[wind] == null;
    }

    public void ForAll(Action<Ivec3> action) => WorldData<SETTINGS, ARRAY, DATA>.StaticForAll(action);
    public void ForAll(Action<int, int> action) => WorldData<SETTINGS, ARRAY, DATA>.StaticForAll(action);
}

public class FastSparseWorldData<SETTINGS, DATA> : SparseWorldData<SETTINGS, FastArray3d<DATA>, DATA>
    where SETTINGS : IWorldSettings, new()
    where DATA : new() {

    private static FastArray3d<DATA> Initer() => new(settings.ChunkBitSize);

    public FastSparseWorldData() : base(Initer) { }
}


public class SparseWorldBoolData<SETTINGS> : SparseWorldData<SETTINGS, BoolArray3d, bool> where SETTINGS : IWorldSettings, new() {
    private static BoolArray3d Initer() => new();

    public SparseWorldBoolData() : base(Initer) { }


}


