using Godot;
using System;

namespace VoxelGame.scripts.common;

public class Array3D<OBJ> {
    protected readonly OBJ[] array;
    protected readonly uint rowLength;
    protected readonly uint planeLength;
    protected readonly uint totalLength;
    public readonly Vector3T<uint> Size;

    public uint Length { get => totalLength; }

    public Array3D(Vector3T<uint> size) {
        rowLength = size.X;
        planeLength = size.X * size.Y;
        totalLength = size.X * size.Y * size.Z;
        Size = size;
        array = new OBJ[totalLength];
    }

    public void Initialize() => array.Initialize();

    public virtual uint GetIndexFromXyz(uint x, uint y, uint z) {
        uint index = x + y * rowLength + z * planeLength;

        if (index < 0 || totalLength <= index) {
            GD.Print($"oob index:{x}:{y}:{z} while s:{Size.X}:{Size.Y}:{Size.Z}");
        }

        return index;
    }

    public virtual Vector3T<uint> GetXyzFromIndex(uint it) {
        uint z = it;
        uint y = z % planeLength;
        uint x = y % rowLength;
        z /= planeLength;
        y /= rowLength;
        return new Vector3T<uint>(x, y, z);
    }

    public OBJ this[Vector3T<long> xyz] {
        get => array[GetIndexFromXyz((uint)xyz.X, (uint)xyz.Y, (uint)xyz.Z)];
        set => array[GetIndexFromXyz((uint)xyz.X, (uint)xyz.Y, (uint)xyz.Z)] = value;
    }

    public OBJ this[uint x, uint y, uint z] {
        get => array[GetIndexFromXyz(x, y, z)];
        set => array[GetIndexFromXyz(x, y, z)] = value;
    }

    public OBJ this[Vector3T<uint> xyz] {
        get => array[GetIndexFromXyz(xyz.X, xyz.Y, xyz.Z)];
        set => array[GetIndexFromXyz(xyz.X, xyz.Y, xyz.Z)] = value;
    }

    public OBJ this[uint i] {
        get => array[i];
        set => array[i] = value;
    }

    public void ForAll(Action<Vector3T<long>> action) => ForAll(Size, action);


    public static void ForAll(Vector3T<uint> size, Action<Vector3T<long>> action) {
        for (long itx = 0; itx < size.X; itx++) {
            for (long ity = 0; ity < size.Y; ity++) {
                for (long itz = 0; itz < size.Z; itz++) {
                    action(new Vector3T<long>(itx, ity, itz));
                }
            }
        }
    }
}
