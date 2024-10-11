using System;
using System.Runtime.CompilerServices;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

using Ivec3 = Vector3T<int>;
public abstract class IWorldSettings {
    public abstract Ivec3 GridSize { get; }
    public abstract Ivec3 GridCenter { get; }
    public abstract Ivec3 ChunkSize { get; }
    public abstract Vector3T<int> ChunkBitSize { get; }

    public Ivec3 TotalMaxs => ((GridSize - GridCenter) * ChunkSize) - 1;
    public Ivec3 TotalMins => -GridCenter * ChunkSize;
    public Ivec3 TotalCenter => GridCenter * ChunkSize;
    public Ivec3 TotalSize => GridSize * ChunkSize;
    public Ivec3 GridMins => -GridCenter;
    public Ivec3 GridMaxs => GridSize - GridCenter - 1;
}

public class WorldData<SETTINGS, ARRAY, DATA>
    where SETTINGS : IWorldSettings, new()
    where ARRAY : IArray3d<DATA> {
    protected static readonly SETTINGS settings = new();
    public SETTINGS Settings { get; } = settings;

    public CenteredArray3D<ARRAY> Chunks { get; }
    protected WorldData(Func<ARRAY> initer) {
        Chunks = new(settings.GridSize, settings.GridCenter);
        Chunks.InitAll((i) => initer());
    }

    public WorldData(Func<ARRAY> initer, Func<Ivec3, DATA> filler) : this(initer) {
        ForAll((xyz) => this[xyz] = filler(xyz));
    }
    public WorldData(Func<ARRAY> initer, Func<int, int, DATA> filler) : this(initer) {
        ForAll((wind, cind) => this[wind, cind] = filler(wind, cind));
    }

    protected static WorldData<SETTINGS, ARRAY, DATA> UnsafeNew(Func<ARRAY> initer) => new(initer);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeconstructPos(Ivec3 pos, out Ivec3 wpos, out Ivec3 cpos) {
        //if (chunkSizeIs4) {
        cpos = pos.And(0b11);
        wpos = pos.ArithmRightShift(2);
        //cpos = pos.Do((val) => val & 0b11);
        //wpos = pos.Do(cpos, (val, cval) => (val - cval) >> 2);
        //} else {
        //    cpos = pos.Modulo(settings.ChunkSize);
        //    wpos = (pos - cpos) / settings.ChunkSize;
        //}
    }

    //private static readonly bool chunkSizeIs4 = settings.ChunkSize == 4;
    private static readonly int chunkBitSizeX = settings.ChunkBitSize.X;
    private static readonly int chunkBitSizeXY = settings.ChunkBitSize.X + settings.ChunkBitSize.Y;
    private static readonly Ivec3 gridCenter = settings.GridCenter;
    private static readonly int gridRow = settings.GridSize.X;
    private static readonly int gridPlane = settings.GridSize.X * settings.GridSize.Y;

    public void DeconstructPosToIndex(Ivec3 pos, out int wind, out int cind) {
        DeconstructPos(pos, out var wpos, out var cpos);

        wind = Chunks.GetIndexFromXyz(wpos);
        cind = Chunks[wind].GetIndexFromXyz(cpos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int wind, int cind) StaticDeconstructPosToIndex(Ivec3 pos /*, out int wind, out int cind*/) {
        Ivec3 cpos = pos.And(0b11);
        Ivec3 wpos = pos.ArithmRightShift(2);

        int wind = CenteredArray3D.GetIndexFromXyz(wpos, gridCenter, gridRow, gridPlane);
        int cind = FastArray3d.GetIndexFromXyz(cpos, chunkBitSizeX, chunkBitSizeXY); //TODO not certain to work, oh well
        return (wind, cind);
    }

    public DATA this[Ivec3 xyz] {
        get {
            DeconstructPos(xyz, out var wpos, out var cpos);
            return Chunks[wpos][cpos];
        }
        set {
            DeconstructPos(xyz, out var wpos, out var cpos);
            Chunks[wpos][cpos] = value;
        }
    }
    public DATA this[int wind, int cind] {
        get => Chunks[wind][cind];
        set => Chunks[wind][cind] = value;
    }

    public void ForAll(Action<Ivec3> action) {
        for (int itx = settings.TotalMins.X; itx <= settings.TotalMaxs.X; itx++) {
            for (int ity = settings.TotalMins.Y; ity <= settings.TotalMaxs.Y; ity++) {
                for (int itz = settings.TotalMins.Z; itz <= settings.TotalMaxs.Z; itz++) {
                    action(new(itx, ity, itz));
                }
            }
        }
    }

    public void ForAll(Action<int, int> action) {
        for (int wind = 0; wind < settings.GridSize.Product(); wind++) {
            for (int cind = 0; cind < settings.ChunkSize.Product(); cind++) {
                action(wind, cind);
            }
        }
    }
}

public class FastWorldData<SETTINGS, DATA> : WorldData<SETTINGS, FastArray3d<DATA>, DATA> where SETTINGS : IWorldSettings, new() {

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


