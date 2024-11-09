using System;
using VoxelGame.scripts.common.arrays;
using VoxelGame.scripts.common.math;

namespace VoxelGame.scripts.content.worlddata;

using Ivec3 = Vector3T<int>;

public class FastWorldData<SETTINGS, DATA> : WorldData<SETTINGS, FastArray3d<DATA>, DATA> where SETTINGS : IWorldSettings, new()
{

    private static FastArray3d<DATA> Initer() => new(settings.ChunkBitSize);

    protected FastWorldData() : base(Initer) { }
    public FastWorldData(Func<int, int, DATA> filler) : base(Initer, filler) { }
    public FastWorldData(Func<Ivec3, DATA> filler) : base(Initer, filler) { }
    public static FastWorldData<SETTINGS, DATA> UnsafeNew() => new();

}
public class WorldBoolData<SETTINGS> : WorldData<SETTINGS, BoolArray3d, bool> where SETTINGS : IWorldSettings, new() {
    private static BoolArray3d Initer() => new();

    protected WorldBoolData() : base(Initer) { }
    public WorldBoolData(Func<int, int, bool> filler) : base(Initer, filler) { }
    public WorldBoolData(Func<Ivec3, bool> filler) : base(Initer, filler) { }
    public static WorldBoolData<SETTINGS> UnsafeNew() => new();

}


