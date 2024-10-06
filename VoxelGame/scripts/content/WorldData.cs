using Godot;
using System;
using System.ComponentModel;
using System.Drawing;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

public abstract class IWorldSettings {
    public abstract Vector3T<int> GridSize { get; }
    public abstract Vector3T<int> Center { get; }
    public abstract Vector3T<int> ChunkSize { get; }
    public abstract Vector3T<byte> ChunkBitSize { get; }

    public Vector3T<int> TotalMaxs => ((GridSize - Center) * ChunkSize) - 1;
    public Vector3T<int> TotalMins => -Center * ChunkSize;
    public Vector3T<int> TotalSize => GridSize * ChunkSize;
}

public class WorldData<SETTINGS, ARRAY, DATA>
    where SETTINGS : IWorldSettings, new()
    where ARRAY : IArray3d<DATA> {
    protected static readonly SETTINGS settings = new();
    public SETTINGS Settings { get; } = settings;

    public CenteredArray3D<ARRAY> Chunks { get; }
    protected WorldData(Func<ARRAY> initer) {
        Chunks = new(settings.GridSize, settings.Center);
        Chunks.InitAll((i) => initer());
    }

    public WorldData(Func<ARRAY> initer, Func<Vector3T<int>, DATA> filler) : this(initer) {
        ForAll((xyz) => this[xyz] = filler(xyz));
    }
    public WorldData(Func<ARRAY> initer, Func<int, int, DATA> filler) : this(initer) {
        ForAll((wind, cind) => this[wind, cind] = filler(wind, cind));
    }

    protected static WorldData<SETTINGS, ARRAY, DATA> UnsafeNew(Func<ARRAY> initer) => new(initer);



    public static void DeconstructPos(Vector3T<int> pos, out Vector3T<int> wpos, out Vector3T<int> cpos) {
        if (settings.ChunkSize == 4) {
            cpos = pos.Do((val) => val & 0b11);
            wpos = pos.Do(cpos, (val, cval) => (val - cval) >> 2);
            return;
        } else {
            cpos = pos.Modulo(settings.ChunkSize);
            wpos = (pos - cpos) / settings.ChunkSize;
            return;
        }
    }


    public void DeconstructPosToIndex(Vector3T<int> pos, out int wind, out int cind) {
        DeconstructPos(pos, out var wpos, out var cpos);
        wind = Chunks.GetIndexFromXyz(wpos);
        cind = Chunks[wind].GetIndexFromXyz(cpos);
    }

    public DATA this[Vector3T<int> xyz] {
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

    public void ForAll(Action<Vector3T<int>> action) {
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
    public FastWorldData(Func<Vector3T<int>, DATA> filler) : base(Initer, filler) { }
    public static FastWorldData<SETTINGS, DATA> UnsafeNew() => new();

}


public class WorldBoolData<SETTINGS> : WorldData<SETTINGS, BoolArray3d, bool> where SETTINGS : IWorldSettings, new() {
    private static BoolArray3d Initer() => new();

    protected WorldBoolData() : base(Initer) { }
    public WorldBoolData(Func<int, int, bool> filler) : base(Initer, filler) { }
    public WorldBoolData(Func<Vector3T<int>, bool> filler) : base(Initer, filler) { }
    public static WorldBoolData<SETTINGS> UnsafeNew() => new();

}


