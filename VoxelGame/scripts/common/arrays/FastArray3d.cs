using Godot;
using System;
using VoxelGame.scripts.common.math;

namespace VoxelGame.scripts.common.arrays;

public class FastArray3d<OBJ> : IArray3d<OBJ>
{
    protected readonly OBJ[] data;

    public Vector3T<int> Size { get; }
    public readonly Vector3T<int> BitSize;
    protected readonly int BitSizeXY;
    protected readonly Vector3T<int> Masks = new();

    public FastArray3d(Vector3T<int> bsize)
    {
        BitSize = bsize;
        BitSizeXY = bsize.X + bsize.Y;
        Masks = Size - 1;
        Masks.Y <<= BitSize.X;
        Masks.Z <<= BitSizeXY;
        Size = bsize.Do((val) => 1 << val);
        data = new OBJ[Size.Product()];
    }

    public FastArray3d(Vector3T<int> bsize, int wind, Func<int, int, OBJ> filler) : this(bsize)
    {
        for (int cind = 0; cind < Size.Product(); cind++)
        {
            this[cind] = filler(wind, cind);
        }
    }

    public int GetIndexFromXyz(Vector3T<int> xyz) => xyz.X | xyz.Y << BitSize.X | xyz.Z << BitSizeXY;

    public Vector3T<int> GetXyzFromIndex(int it)
    {
        var rtn = Masks.And(it);
        rtn.Y >>>= BitSize.X;
        rtn.Z >>>= BitSizeXY;
        return rtn;
    }


    public OBJ this[Vector3T<int> xyz]
    {
        get => data[GetIndexFromXyz(xyz)];
        set => data[GetIndexFromXyz(xyz)] = value;
    }

    public OBJ this[int i]
    {
        get => data[i];
        set => data[i] = value;
    }
}

public static class FastArray3d
{
    public static int GetIndexFromXyz(Vector3T<int> xyz, int bitsizex, int bitsizexy) => xyz.X | xyz.Y << bitsizex | xyz.Z << bitsizexy;
}
