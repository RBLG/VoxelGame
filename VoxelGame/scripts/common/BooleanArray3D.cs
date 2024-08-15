using Godot;
using System;
using System.Numerics;

namespace VoxelGame.scripts.common;

public class BooleanArray3D {
    protected ulong data = 0;
    public readonly Vector3T<uint> Size = new(4);
    protected readonly Vector3T<uint> Masks = new(3, 3 << 2, 3 << 4);

    public BooleanArray3D() { }
    public static int GetIndexFromXyz(uint x, uint y, uint z) => (int)(x | (y << 2) | (z << 4));

    public Vector3T<uint> GetXyzFromIndex(uint it) {
        var rtn = Masks.Do((m) => it & m);
        rtn.Y >>= 2;
        rtn.Z >>= 4;
        return rtn;
    }

    public bool this[Vector3T<long> xyz] {
        get => this[(uint)xyz.X, (uint)xyz.Y, (uint)xyz.Z];
        set => this[(uint)xyz.X, (uint)xyz.Y, (uint)xyz.Z] = value;
    }
    public bool this[Vector3T<uint> xyz] {
        get => this[xyz.X, xyz.Y, xyz.Z];
        set => this[xyz.X, xyz.Y, xyz.Z] = value;
    }
    public bool this[uint x, uint y, uint z] {
        get => this[GetIndexFromXyz(x, y, z)];
        set => this[GetIndexFromXyz(x, y, z)] = value;
    }
    public bool this[uint i] {
        get => this[(int)i];
        set => this[(int)i] = value;
    }

    public bool this[int i] {
        get => (data & (1UL << i)) != 0;
        set {
            if (value) { data |= 1UL << i; } else { data &= ~(1UL << i); }
        }
    }

    public int Sum() => BitOperations.PopCount(data);


    public void ForAll(Action<Vector3T<long>> action) => Array3D<object>.ForAll(Size, action);

}
