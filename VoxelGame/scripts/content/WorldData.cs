using Godot;
using System;
using System.ComponentModel;
using System.Drawing;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;

public interface IWorldSettings {
    public Vector3T<int> Size { get; }
    public Vector3T<int> Center { get; }
    public Vector3T<int> ChunkSize { get; }
    public Vector3T<byte> ChunkBitSize { get; }
}

public class WorldData<SETTINGS, ARRAY, DATA>
    where SETTINGS : IWorldSettings, new()
    where ARRAY : IArray3d<DATA> {

    protected static readonly SETTINGS settings = new();
    public CenteredArray3D<ARRAY> Chunks { get; }
    public Vector3T<int> Maxs { get; }
    public Vector3T<int> Mins { get; }
    public Vector3T<int> TotalSize { get; }

    protected WorldData(Func<ARRAY> initer) {
        Chunks = new(settings.Size, settings.Center);
        Chunks.InitAll((i) => initer());
        Maxs = ((settings.Size - settings.Center) * settings.ChunkSize) - 1;
        Mins = -settings.Center * settings.ChunkSize;
        TotalSize = settings.Size * settings.ChunkSize;
    }

    public WorldData(Func<ARRAY> initer, Func<Vector3T<int>, DATA> filler) : this(initer) {
        ForAll((xyz) => this[xyz] = filler(xyz));
    }
    public WorldData(Func<ARRAY> initer, Func<int, int, DATA> filler) : this(initer) {
        ForAll((wind, cind) => this[wind, cind] = filler(wind, cind));
    }

    protected static WorldData<SETTINGS, ARRAY, DATA> UnsafeNew(Func<ARRAY> initer) => new(initer);


    public void DeconstructPos(Vector3T<int> pos, out Vector3T<int> wpos, out Vector3T<int> cpos) {
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
        for (int itx = Mins.X; itx <= Maxs.X; itx++) {
            for (int ity = Mins.Y; ity <= Maxs.Y; ity++) {
                for (int itz = Mins.Z; itz <= Maxs.Z; itz++) {
                    action(new(itx, ity, itz));
                }
            }
        }
    }

    public void ForAll(Action<int, int> action) {
        int wlen = settings.Size.Product();
        int clen = settings.ChunkSize.Product();
        for (int wind = 0; wind < wlen; wind++) {
            for (int cind = 0; cind < clen; cind++) {
                action(wind, cind);
            }
        }
    }
}



public class WorldBinaryData<SETTINGS, DATA> : WorldData<SETTINGS, BinaryArray3d<DATA>, DATA> where SETTINGS : IWorldSettings, new() {

    private static BinaryArray3d<DATA> Initer() => new(settings.ChunkBitSize);

    protected WorldBinaryData() : base(Initer) { }
    public WorldBinaryData(Func<int, int, DATA> filler) : base(Initer, filler) { }
    public WorldBinaryData(Func<Vector3T<int>, DATA> filler) : base(Initer, filler) { }
    public static WorldBinaryData<SETTINGS, DATA> UnsafeNew() => new();

}


public class WorldBoolData<SETTINGS> : WorldData<SETTINGS, BooleanArray3D, bool> where SETTINGS : IWorldSettings, new() {
    private static BooleanArray3D Initer() => new();

    protected WorldBoolData() : base(Initer) { }
    public WorldBoolData(Func<int, int, bool> filler) : base(Initer, filler) { }
    public WorldBoolData(Func<Vector3T<int>, bool> filler) : base(Initer, filler) { }
    public static WorldBoolData<SETTINGS> UnsafeNew() => new();

}


