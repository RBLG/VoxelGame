using Godot;
using System;

namespace VoxelGame.scripts.common;

public class BinaryArray3D<OBJ> : Array3D<OBJ> {
    public readonly Vector3T<byte> BitSize;
    protected readonly int BitSizeXY;

    protected readonly Vector3T<uint> Masks = new();

    public BinaryArray3D(Vector3T<byte> bsize) : base(bsize.Do((val) => 1u << val)) {
        BitSize = bsize;
        BitSizeXY = bsize.X + bsize.Y;
        Masks = Size - 1;
        Masks.Y <<= BitSize.X;
        Masks.Z <<= BitSizeXY;
    }
    public override uint GetIndexFromXyz(uint x, uint y, uint z) => x | (y << BitSize.X) | (z << BitSizeXY);

    public override Vector3T<uint> GetXyzFromIndex(uint it) {
        var rtn = Masks.Do((m) => it & m);
        rtn.Y >>= BitSize.X;
        rtn.Z >>= BitSizeXY;
        return rtn;
    }
    public static uint GetIndexFromXyz(uint x, uint y, uint z, int bsizex, int bsizey) => x | (y << bsizex) | (z << (bsizex + bsizey));
}
