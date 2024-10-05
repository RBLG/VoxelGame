using Godot;
using System;
using VoxelGame.scripts.common;

namespace VoxelGame.scripts.content;
public class World {

    public readonly WorldBinaryData<WorldSettings1, Voxel> chunks;
    public readonly WorldBoolData1 Opacity;
    public readonly WorldBinaryData<WorldSettings1, Bool8Array> Adjacency;

    public World() {
        chunks = new((wind, cind) => new());
        Opacity = new((wind, cind) => new());
        Adjacency = new((wind, cind) => new());
    }

    public void Generate(WorldGenerator1 generator) {
        chunks.Chunks.ForAll((xyz) => {
            chunks.Chunks[xyz] = generator.GenerateChunk(xyz, out var OpacityChunk);
            Opacity.Chunks[xyz] = OpacityChunk;
        });
        generator.InsertFeatures(this);
        chunks.ForAll((xyz) => {
            bool a1 = (xyz.X != chunks.Maxs.X) && Opacity[xyz + (1, 0, 0)];
            bool a2 = (xyz.Y != chunks.Maxs.Y) && Opacity[xyz + (0, 1, 0)];
            bool a3 = (xyz.Z != chunks.Maxs.Z) && Opacity[xyz + (0, 0, 1)];
            bool a4 = (xyz.X != chunks.Mins.X) && Opacity[xyz - (1, 0, 0)];
            bool a5 = (xyz.Y != chunks.Mins.Y) && Opacity[xyz - (0, 1, 0)];
            bool a6 = (xyz.Z != chunks.Mins.Z) && Opacity[xyz - (0, 0, 1)];
            Adjacency[xyz].Set(a1, a2, a3, a4, a5, a6);
        });
    }

    public void WriteToTexture2DArrays(Texture2DArray occup, Texture2DArray colors) {
        for (int itz = 0; itz < chunks.TotalSize.Z; itz++) {
            Image layer = colors.GetLayerData(itz);
            for (int itx = 0; itx < chunks.TotalSize.X; itx++) {
                for (int ity = 0; ity < chunks.TotalSize.Y; ity++) {
                    var xyz = chunks.Mins + (itx, ity, itz);
                    chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);

                    var fcol = chunks[wind, cind].color;
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
        var mins = -chunks.Chunks.Center;
        for (int itz = 0; itz < chunks.Chunks.Size.Z; itz++) {
            Image layer = occup.GetLayerData(itz);

            for (int itx = 0; itx < chunks.Chunks.Size.X; itx++) {
                for (int ity = 0; ity < chunks.Chunks.Size.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

                    ulong occupancy = Opacity.Chunks[xyz].Data;

                    layer.SetPixel(itx, ity, new() {
                        R = BitConverter.UInt32BitsToSingle((uint)occupancy),
                        G = BitConverter.UInt32BitsToSingle((uint)(occupancy >> 32))
                    });
                }
            }
            occup.UpdateLayer(layer, itz);
        }
    }

    public static World From(Texture2DArray occupancy, Texture2DArray colors) {
        //Vector3T<int> size = new(occupancy.GetWidth(), occupancy.GetHeight(), occupancy.GetLayers());
        World world = new();

        for (int itz = 0; itz < world.chunks.TotalSize.Z; itz++) {
            Image layer = colors.GetLayerData(itz);
            for (int itx = 0; itx < world.chunks.TotalSize.X; itx++) {
                for (int ity = 0; ity < world.chunks.TotalSize.Y; ity++) {
                    var xyz = world.chunks.Mins + (itx, ity, itz);

                    Color data = layer.GetPixel(itx, ity);
                    world.chunks[xyz].color = new(data.R, data.G, data.B);
                }
            }
        }
        var mins = -world.chunks.Chunks.Center;
        for (int itz = 0; itz < world.chunks.Chunks.Size.Z; itz++) {
            Image layer = occupancy.GetLayerData(itz);

            for (int itx = 0; itx < world.chunks.Chunks.Size.X; itx++) {
                for (int ity = 0; ity < world.chunks.Chunks.Size.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

                    Color data = layer.GetPixel(itx, ity);
                    ulong data1 = BitConverter.SingleToUInt32Bits(data.R);
                    ulong data2 = ((ulong)BitConverter.SingleToUInt32Bits(data.G)) << 32;

                    world.Opacity.Chunks[xyz].Data = data1 | data2;
                }
            }
        }




        return world;
    }
}

public class WorldSettings1 : IWorldSettings {
    public Vector3T<int> Size => new(40, 40, 20);
    public Vector3T<int> Center => Size / 2;
    public Vector3T<int> ChunkSize => new(4);
    public Vector3T<byte> ChunkBitSize => new(2);
}

public record class Voxel {
    public Vector3T<float> color;
    //public bool Opaque = false;
    public Vector2T<byte> uv = new(3, 3);
    public Vector3T<byte> emit; //just a float?
}

public class WorldBoolData1 : WorldBoolData<WorldSettings1> {
    protected WorldBoolData1() : base() { }
    public WorldBoolData1(Func<int, int, bool> filler) : base(filler) { }
    public WorldBoolData1(Func<Vector3T<int>, bool> filler) : base(filler) { }
    public new static WorldBoolData1 UnsafeNew() => new();

}

//[Obsolete("just bad")]
public class WorldData1<DATA> : WorldBinaryData<WorldSettings1, DATA> {

    protected WorldData1() : base() { }
    public WorldData1(Func<int, int, DATA> filler) : base(filler) { }
    public WorldData1(Func<Vector3T<int>, DATA> filler) : base(filler) { }
    public new static WorldData1<DATA> UnsafeNew() => new();

}
