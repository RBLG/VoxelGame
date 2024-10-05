using Godot;
using System;
using VoxelGame.scripts.content;

namespace VoxelGame.scripts.common;

public class CenteredArray3D<OBJ> : IArray3d<OBJ> {
    private readonly OBJ[] data;
    private readonly int rowLength;
    private readonly int planeLength;
    private readonly int totalLength;
    public Vector3T<int> Size { get; }
    public Vector3T<int> Center { get; }

    public int Length { get => totalLength; }

    public OBJ[] Data => data;

    public CenteredArray3D(Vector3T<int> size) : this(size, new(0)) { }
    public CenteredArray3D(Vector3T<int> size, Vector3T<int> center) {
        rowLength = size.X;
        planeLength = size.X * size.Y;
        totalLength = size.X * size.Y * size.Z;
        Size = size;
        Center = center;
        data = new OBJ[totalLength];
    }

    public void Initialize() => data.Initialize();

    public int GetIndexFromXyz(Vector3T<int> xyz) {
        int x2 = xyz.X + Center.X;
        int y2 = xyz.Y + Center.Y;
        int z2 = xyz.Z + Center.Z;
        int index = x2 + y2 * rowLength + z2 * planeLength;

        if (index < 0 || totalLength <= index) {
            GD.Print($"xyz:{x2}:{y2}:{z2} while c:{Center.X}:{Center.Y}:{Center.Z} and s:{Size.X}:{Size.Y}:{Size.Z}");
        }

        return index;
    }

    public Vector3T<int> GetXyzFromIndex(int it) {
        int z = it;
        int y = z % planeLength;
        int x = y % rowLength;
        z /= planeLength;
        y /= rowLength;
        return -Center + (x, y, z);
    }

    public OBJ this[Vector3T<int> xyz] {
        get => this[GetIndexFromXyz(xyz)];
        set => this[GetIndexFromXyz(xyz)] = value;
    }

    public OBJ this[int i] {
        get => data[i];
        set => data[i] = value;
    }

    public void ForAll(Action<Vector3T<int>> action) => IArray3d<OBJ>.ForAll(Size, (xyz) => action(xyz - Center));

    public void InitAll(Func<int, OBJ> filler) {
        for (int it = 0; it < totalLength; it++) {
            data[it] = filler(it);
        }
    }
}
