using Godot;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;
public class WorldGenerator1 {
    public Chunk GenerateChunk(Vector3T<long> pos, out BooleanArray3D OpacityData) {
        Chunk nchunk = new();
        BooleanArray3D Opacities = new();
        nchunk.voxels.ForAll((xyz) => {
            Vector3T<long> xyz2 = xyz + pos * Chunk.LSize;
            nchunk.voxels[xyz] = GenerateVoxel(xyz2, out var opaque);
            Opacities[xyz] = opaque;
        });
        OpacityData = Opacities;
        return nchunk;
    }

    public Voxel GenerateVoxel(Vector3T<long> pos, out bool Opaque) {
        bool opaque = !(-10 < pos.Z && GD.Randf() < 0.995);
        opaque &= (pos.X != 0 && pos.X != -1) || (pos.Y != 0 && pos.Y != -1);

        //Vector3T<float> col = new(GD.Randf(), GD.Randf(), GD.Randf());
        Vector3T<float> col = new(1, 1, 1);
        col *= 0.3f;
        //col *= 255;
        Voxel nvox = new() {
            color = opaque ? col : new(0),//.Do((val) => (byte)val) : new(0),
            //Opaque = opaque
        };
        Opaque = opaque;
        return nvox;
    }
}

