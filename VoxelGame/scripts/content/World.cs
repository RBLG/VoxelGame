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
        Voxels.ForAll((xyz) => {
            bool a1 = (xyz.X != settings.TotalMaxs.X) && Occupancy[xyz + (1, 0, 0)];
            bool a2 = (xyz.Y != settings.TotalMaxs.Y) && Occupancy[xyz + (0, 1, 0)];
            bool a3 = (xyz.Z != settings.TotalMaxs.Z) && Occupancy[xyz + (0, 0, 1)];
            bool a4 = (xyz.X != settings.TotalMins.X) && Occupancy[xyz - (1, 0, 0)];
            bool a5 = (xyz.Y != settings.TotalMins.Y) && Occupancy[xyz - (0, 1, 0)];
            bool a6 = (xyz.Z != settings.TotalMins.Z) && Occupancy[xyz - (0, 0, 1)];
            Adjacency[xyz].Set(a1, a2, a3, a4, a5, a6);
        });
    }

    public void Export(Texture2DArray occup, Texture2DArray colors) {
        for (int itz = 0; itz < settings.TotalSize.Z; itz++) {
            Image layer = colors.GetLayerData(itz);
            for (int itx = 0; itx < settings.TotalSize.X; itx++) {
                for (int ity = 0; ity < settings.TotalSize.Y; ity++) {
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
        var mins = -Voxels.Chunks.Center;
        for (int itz = 0; itz < Voxels.Chunks.Size.Z; itz++) {
            Image layer = occup.GetLayerData(itz);

            for (int itx = 0; itx < Voxels.Chunks.Size.X; itx++) {
                for (int ity = 0; ity < Voxels.Chunks.Size.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

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
        if (!(settings.TotalSize == (colors.GetLayers(), colors.GetHeight(), colors.GetWidth()))) {
            GD.Print($"world import failed: occupancy file doesnt fit settings ({settings.TotalSize.X}/{settings.TotalSize.Y}/{settings.TotalSize.Z})");
            return world;
        }else if (!(world.Occupancy.Chunks.Size == (occupancy.GetLayers(), occupancy.GetHeight(), occupancy.GetWidth()))) {
            var size2 = world.Occupancy.Chunks.Size;
            GD.Print($"world import failed: colors file doesnt fit settings ({size2.X}/{size2.Y}/{size2.Z})");
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
        var chunkSize = world.Occupancy.Chunks.Size;
        var mins = -world.Occupancy.Chunks.Center;
        for (int itz = 0; itz < chunkSize.Z; itz++) {
            Image layer = occupancy.GetLayerData(itz);
            for (int itx = 0; itx < chunkSize.X; itx++) {
                for (int ity = 0; ity < chunkSize.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

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
    public override Vector3T<int> Center => GridSize / 2;
    public override Vector3T<int> ChunkSize => new(4);
    public override Vector3T<byte> ChunkBitSize => new(2);
}

public class Voxel {
    public Vector3T<float> color;
    public Vector2T<byte> uv = new(3, 3);
    public Vector3T<byte> emit; //just a float?
}




[Obsolete("use WorldBoolData<WorldSettings1> and aliases")]
public class WorldBoolData1 : WorldBoolData<WorldSettings1> {
    protected WorldBoolData1() : base() { }
    public WorldBoolData1(Func<int, int, bool> filler) : base(filler) { }
    public WorldBoolData1(Func<Vector3T<int>, bool> filler) : base(filler) { }
    public new static WorldBoolData1 UnsafeNew() => new();

}

[Obsolete("use FastWorldData<WorldSettings1, DATA> and aliases")]
public class WorldData1<DATA> : FastWorldData<WorldSettings1, DATA> {

    protected WorldData1() : base() { }
    public WorldData1(Func<int, int, DATA> filler) : base(filler) { }
    public WorldData1(Func<Vector3T<int>, DATA> filler) : base(filler) { }
    public new static WorldData1<DATA> UnsafeNew() => new();

}


