using Godot;
using System;

namespace VoxelGame.scripts.common;

public class Array3d<OBJ> : IArray3d<OBJ> {
    protected readonly OBJ[] data;
    protected readonly int rowLength;
    protected readonly int planeLength;
    protected readonly int totalLength;
    public Vector3T<int> Size { get; }

    public int Length { get => totalLength; }

    public Array3d(Vector3T<int> size) {
        rowLength = size.X;
        planeLength = size.X * size.Y;
        totalLength = size.X * size.Y * size.Z;
        Size = size;
        data = new OBJ[totalLength];
    }

    public virtual int GetIndexFromXyz(Vector3T<int> xyz) {
        int index = xyz.X + xyz.Y * rowLength + xyz.Z * planeLength;

        if (index < 0 || totalLength <= index) {
            GD.Print($"oob index:{xyz.X}:{xyz.Y}:{xyz.Z} while s:{Size.X}:{Size.Y}:{Size.Z}");
        }

        return index;
    }

    public virtual Vector3T<int> GetXyzFromIndex(int it) {
        int z = it;
        int y = z % planeLength;
        int x = y % rowLength;
        z /= planeLength;
        y /= rowLength;
        return new Vector3T<int>(x, y, z);
    }

    public OBJ this[Vector3T<int> xyz] {
        get => data[GetIndexFromXyz(xyz)];
        set => data[GetIndexFromXyz(xyz)] = value;
    }


    public OBJ this[int i] {
        get => data[i];
        set => data[i] = value;
    }


    public void InitAll(Func<int, OBJ> filler) {
        for (int it = 0; it < totalLength; it++) {
            data[it] = filler(it);
        }
    }
}
