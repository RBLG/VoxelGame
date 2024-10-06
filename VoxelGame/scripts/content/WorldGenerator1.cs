using Godot;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;
public class WorldGenerator1 {
    WorldSettings1 settings = new();

    public FastArray3d<Voxel> GenerateChunk(Vector3T<int> pos, out BoolArray3d OpacityData) {
        FastArray3d<Voxel> nchunk = new(new(2));
        BoolArray3d Opacities = new();
        IArray3d<Voxel> nchunk2 = nchunk;
        nchunk2.ForAll((xyz) => {
            int ind = nchunk.GetIndexFromXyz(xyz);
            Vector3T<int> xyz2 = xyz + pos * settings.ChunkSize;
            nchunk[ind] = GenerateVoxel(xyz2, out var opaque);
            Opacities[ind] = opaque;
        });
        OpacityData = Opacities;
        return nchunk;
    }

    public Voxel GenerateVoxel(Vector3T<int> pos, out bool Opaque) {
        bool opaque = pos.Z <= -10;

        long tot = (pos.X & 63L) + (pos.Y & 63L);
        //opaque |= (pos.Z < tot - 24);

        if (!opaque) {
            float dist = Mathf.Sqrt(pos.LengthSquared()) * 0.02f;
            float chance = 1.05f - (dist / (1 + dist))*0.1f;
            opaque |= chance < GD.Randf();
        }


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

    public void InsertFeatures(World world) {
        for (int itx = 20; itx < 53; itx++) {
            for (int itz = -10; itz < 13; itz++) {
                Vector3T<int> xyz = new(itx, -30, itz);

                world.Voxels[xyz].color = new(0.8f, 0.7f, 0.6f);
                world.Occupancy[xyz] = true;
            }
        }

        for (int itx = 15; itx < 53; itx++) {
            for (int itz = -10; itz < 20; itz++) {
                Vector3T<int> xyz = new(itx, -40, itz);

                world.Voxels[xyz].color = new(0.8f, 0.7f, 0.6f);
                world.Occupancy[xyz] = true;
            }
        }

    }
}

