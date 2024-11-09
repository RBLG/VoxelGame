using Godot;
using System;
using VoxelGame.scripts.common.math;
using VoxelGame.scripts.content;

namespace VoxelGame.scripts.common.arrays;

public class CenteredArray3D<OBJ> : IArray3d<OBJ>
{
    private readonly OBJ[] data;
    private readonly int rowLength;
    private readonly int planeLength;
    private readonly int totalLength;
    public Vector3T<int> Size { get; }
    public Vector3T<int> Center { get; }

    public int Length => totalLength;

    public OBJ[] Data => data;

    public CenteredArray3D(Vector3T<int> size) : this(size, new(0)) { }
    public CenteredArray3D(Vector3T<int> size, Vector3T<int> center)
    {
        rowLength = size.X;
        planeLength = size.X * size.Y;
        totalLength = size.X * size.Y * size.Z;
        Size = size;
        Center = center;
        data = new OBJ[totalLength];
    }

    public int GetIndexFromXyz(Vector3T<int> xyz)
    {
        xyz += Center;
        return xyz.X + xyz.Y * rowLength + xyz.Z * planeLength;
        //return ((xyz + Center) * (1, rowLength, planeLength)).Sum();
    }


    public Vector3T<int> GetXyzFromIndex(int it)
    {
        int z = it;
        int y = z % planeLength;
        int x = y % rowLength;
        z /= planeLength;
        y /= rowLength;
        return -Center + (x, y, z);
    }

    public OBJ this[Vector3T<int> xyz]
    {
        get => this[GetIndexFromXyz(xyz)];
        set => this[GetIndexFromXyz(xyz)] = value;
    }

    public OBJ this[int i]
    {
        get => data[i];
        set => data[i] = value;
    }

    public void ForAll(Action<Vector3T<int>> action) => IArray3d<OBJ>.ForAll(Size, (xyz) => action(xyz - Center));

    public void InitAll(Func<int, OBJ> filler)
    {
        for (int it = 0; it < totalLength; it++)
        {
            data[it] = filler(it);
        }
    }
}

public static class CenteredArray3D
{
    public static int GetIndexFromXyz(Vector3T<int> xyz, Vector3T<int> Center, int rowLength, int planeLength)
    {
        xyz += Center;
        return xyz.X + xyz.Y * rowLength + xyz.Z * planeLength;
    }
}