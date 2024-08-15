using Godot;
using System;

namespace VoxelGame.scripts.common;

public class CenteredArray3D<OBJ> {
    private readonly OBJ[] array;
	private readonly uint rowLength;
	private readonly uint planeLength;
	private readonly uint totalLength;
	public readonly Vector3T<uint> Size;
	public readonly Vector3T<long> Center;

	public uint Length { get => totalLength; }

	public CenteredArray3D(Vector3T<uint> size) : this(size, new(0)) { }
	public CenteredArray3D(Vector3T<uint> size, Vector3T<long> center) {
		rowLength = size.X;
		planeLength = size.X * size.Y;
		totalLength = size.X * size.Y * size.Z;
		Size = size;
		Center = center;
		array = new OBJ[totalLength];
	}

	public void Initialize() => array.Initialize();

	public uint GetIndexFromXyz(long x, long y, long z) {
		uint x2 = (uint)(x + Center.X);
		uint y2 = (uint)(y + Center.Y);
		uint z2 = (uint)(z + Center.Z);
		uint index = x2 + y2 * rowLength + z2 * planeLength;

		if (index < 0 || totalLength <= index) {
			GD.Print($"xyz:{x2}:{y2}:{z2} while c:{Center.X}:{Center.Y}:{Center.Z} and s:{Size.X}:{Size.Y}:{Size.Z}");
		}

		return index;
	}

	public Vector3T<long> GetXyzFromIndex(uint it) {
		uint z = it;
		uint y = z % planeLength;
		uint x = y % rowLength;
		z /= planeLength;
		y /= rowLength;
		return new Vector3T<long>(x, y, z) - Center;
	}

	public OBJ this[long x, long y, long z] {
		get => array[GetIndexFromXyz(x, y, z)];
		set => array[GetIndexFromXyz(x, y, z)] = value;
	}

	public OBJ this[Vector3T<long> xyz] {
		get => array[GetIndexFromXyz(xyz.X, xyz.Y, xyz.Z)];
		set => array[GetIndexFromXyz(xyz.X, xyz.Y, xyz.Z)] = value;
	}

	public OBJ this[uint i] {
		get => array[i];
		set => array[i] = value;
	}

	public void ForAll(Action<Vector3T<long>> action) {
		for (long itx = 0; itx < Size.X; itx++) {
			for (long ity = 0; ity < Size.Y; ity++) {
				for (long itz = 0; itz < Size.Z; itz++) {
					action(new Vector3T<long>(itx, ity, itz) - Center);
				}
			}
		}
	}
}
