using Godot;
using System;
using System.Numerics;

namespace VoxelGame.scripts.common;

public class BoolArray3d : IArray3d<bool> {
    protected ulong data = 0;
    public Vector3T<int> Size { get; } = new(4);
    protected static readonly Vector3T<int> Masks = new(3, 3 << 2, 3 << 4);

    public BoolArray3d() { }
    public int GetIndexFromXyz(Vector3T<int> xyz) => xyz.X | (xyz.Y << 2) | (xyz.Z << 4);

    public Vector3T<int> GetXyzFromIndex(int it) {
        var rtn = Masks.Do((m) => it & m);
        rtn.Y >>= 2;
        rtn.Z >>= 4;
        return rtn;
    }
    
    public bool this[Vector3T<int> xyz] {
        get => this[GetIndexFromXyz(xyz)];
        set => this[GetIndexFromXyz(xyz)] = value;
    }

    public bool this[int i] {
        get => (data & (1UL << i)) != 0;
        set {
            if (value) { data |= 1UL << i; } else { data &= ~(1UL << i); }
        }
    }

    public ulong Data { get => data; set => data = value; }

    public int Sum() => BitOperations.PopCount(data);


}
