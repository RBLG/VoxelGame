using Godot;
using System;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;


public class World {
    private static readonly WorldSettings1 settings = new();

    public readonly FastWorldData<WorldSettings1, Voxel> Voxels;
    public readonly WorldBoolData<WorldSettings1> Occupancy;
    public readonly FastWorldData<WorldSettings1, Bool8Pack> Adjacency;

    protected World() {
        Voxels = new((wind, cind) => new());
        Occupancy = new((wind, cind) => new());
        Adjacency = new((wind, cind) => new());
    }

    public static World Generate(WorldGenerator1 generator) {
        World world = new();
        world.Voxels.Chunks.ForAll((xyz) => {
            world.Voxels.Chunks[xyz] = generator.GenerateChunk(xyz, out var OccupancyChunk);
            world.Occupancy.Chunks[xyz] = OccupancyChunk;
        });
        generator.InsertFeatures(world);
        world.UpdateAdjacency();
        return world;
    }

    public static World NewEmpty() => new();

    public void UpdateAdjacency() { //TODO change world initialization schema so that updateAdjacency is always called 
        var totalMax = settings.TotalMaxs;
        var totalMin = settings.TotalMins;
        Voxels.ForAll((xyz) => {
            bool a0 = (xyz.X != totalMax.X) && Occupancy[xyz + (1, 0, 0)];
            bool a1 = (xyz.Y != totalMax.Y) && Occupancy[xyz + (0, 1, 0)];
            bool a2 = (xyz.Z != totalMax.Z) && Occupancy[xyz + (0, 0, 1)];
            bool a3 = (xyz.X != totalMin.X) && Occupancy[xyz - (1, 0, 0)];
            bool a4 = (xyz.Y != totalMin.Y) && Occupancy[xyz - (0, 1, 0)];
            bool a5 = (xyz.Z != totalMin.Z) && Occupancy[xyz - (0, 0, 1)];
            Adjacency[xyz].Set(a0, a1, a2, a3, a4, a5);
        });
    }

    public void Export(Texture2DArray occup, Texture2DArray colors) {
        var totalSize = settings.TotalSize;
        for (int itz = 0; itz < totalSize.Z; itz++) {
            Image layer = colors.GetLayerData(itz);
            for (int itx = 0; itx < totalSize.X; itx++) {
                for (int ity = 0; ity < totalSize.Y; ity++) {
                    var xyz = settings.TotalMins + (itx, ity, itz);
                    Voxels.DeconstructPosToIndex(xyz, out var wind, out var cind);

                    var fcol = Voxels[wind, cind].color;
                    float lum = (fcol * (0.2126f, 0.7152f, 0.0722f)).Sum();
                    fcol /= 1 + lum;
                    fcol = fcol.Clamp(new(0), new(1));

                    layer.SetPixel(itx, ity, new() {
                        R = fcol.X,
                        G = fcol.Y,
                        B = fcol.Z,
                    });
                }
            }
            colors.UpdateLayer(layer, itz);
        }
        var gridSize = settings.GridSize;
        for (int itz = 0; itz < gridSize.Z; itz++) {
            Image layer = occup.GetLayerData(itz);
            for (int itx = 0; itx < gridSize.X; itx++) {
                for (int ity = 0; ity < gridSize.Y; ity++) {
                    var xyz = settings.GridMins + (itx, ity, itz);

                    ulong occupancy = Occupancy.Chunks[xyz].Data;

                    layer.SetPixel(itx, ity, new() {
                        R = BitConverter.UInt32BitsToSingle((uint)occupancy),
                        G = BitConverter.UInt32BitsToSingle((uint)(occupancy >> 32))
                    });
                }
            }
            occup.UpdateLayer(layer, itz);
        }
    }

    public static World Import(Texture2DArray occupancy, Texture2DArray colors) {
        World world = new();

        var colsize = settings.TotalSize;
        var colsimg = new Vector3T<int>(colors.GetWidth(), colors.GetHeight(), colors.GetLayers());
        if (!(colsize == colsimg)) {
            GD.PrintErr($"world import failed: occupancy size was ({colsimg.X}/{colsimg.Y}/{colsimg.Z}) expected ({colsize.X}/{colsize.Y}/{colsize.Z})");
        }
        var occsize = settings.GridSize;
        var occsimg = new Vector3T<int>(occupancy.GetWidth(), occupancy.GetHeight(), occupancy.GetLayers());
        if (!(occsize == occsimg)) {
            GD.PrintErr($"world import failed: colors size was ({occsimg.X}/{occsimg.Y}/{occsimg.Z}) expected ({occsize.X}/{occsize.Y}/{occsize.Z})");
        }
        if (!(colsize == colsimg) || !(occsize == occsimg)) {
            return world;
        }

        var totalSize = settings.TotalSize;
        for (int itz = 0; itz < totalSize.Z; itz++) {
            Image layer = colors.GetLayerData(itz);
            for (int itx = 0; itx < totalSize.X; itx++) {
                for (int ity = 0; ity < totalSize.Y; ity++) {
                    var xyz = settings.TotalMins + (itx, ity, itz);

                    Color data = layer.GetPixel(itx, ity);
                    world.Voxels[xyz].color = new(data.R, data.G, data.B);
                }
            }
        }
        var gridSize = settings.GridSize;
        for (int itz = 0; itz < gridSize.Z; itz++) {
            Image layer = occupancy.GetLayerData(itz);
            for (int itx = 0; itx < gridSize.X; itx++) {
                for (int ity = 0; ity < gridSize.Y; ity++) {
                    var xyz = settings.GridMins + (itx, ity, itz);

                    Color data = layer.GetPixel(itx, ity);
                    ulong data1 = BitConverter.SingleToUInt32Bits(data.R);
                    ulong data2 = ((ulong)BitConverter.SingleToUInt32Bits(data.G)) << 32;

                    world.Occupancy.Chunks[xyz].Data = data1 | data2;
                }
            }
        }
        world.UpdateAdjacency();

        return world;
    }
}

public class WorldSettings1 : IWorldSettings {
    public override Vector3T<int> GridSize => new(40, 40, 20);
    public override Vector3T<int> GridCenter => GridSize / 2;
    public override Vector3T<int> ChunkSize => new(4);
    public override Vector3T<byte> ChunkBitSize => new(2);
}

public class Voxel {
    public Vector3T<float> color;
    public Vector2T<byte> uv = new(3, 3);
    public Vector3T<byte> emit; //just a float?
}

