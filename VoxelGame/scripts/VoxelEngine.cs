using Godot;
using System;
using System.Linq;
using VoxelGame.scripts.common;
using VoxelGame.scripts.content;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace voxelgame.scripts;
public partial class VoxelEngine : MeshInstance3D {

    ShaderMaterial mat = GD.Load<ShaderMaterial>("res://shaders/VoxelEngineMaterial.tres");

    static Vector3T<uint> csize = new(40, 40, 10);
    static Vector3T<uint> size = csize * Chunk.Size;
    static Vector3T<int> isize = size.Do((val) => (int)val);
    private readonly World world = new(csize, new(20, 20, 5));//size.Do((val) => (long)val / 2)

    const uint mask8b = 0b11111111;
    const uint mask3b = 0b111;
    //const uint mask2b = 0b11;
    const uint mask1b = 0b1;

    private readonly WorldBadLightEngine wble;

    public VoxelEngine() : base() {
        wble = new(world);
    }

    public override void _Ready() {
        base._Ready();
        world.Generate(new WorldGenerator1());
        wble.AsyncDoLighting();

        CreateGpuData();
        UpdateGpuData();
        UpdateGpuOpacity();
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if (wble.ApplyLatestResults()) {
            UpdateGpuData();
        }
    }

    private Texture2DArray worldColorsBuffer = new();
    private Texture2DArray worldOpacityBuffer = new();

    public void CreateGpuData() {
        Image[] imgs = new Image[size.Z].Select((i) => {
            return Image.CreateEmpty(isize.X, isize.Y, false, Image.Format.Rgba8);
        }).ToArray();
        _ = worldColorsBuffer.CreateFromImages(new(imgs));

        imgs = new Image[csize.Z].Select((i) => {
            return Image.CreateEmpty((int)csize.X, (int)csize.Y, false, Image.Format.Rgf);
        }).ToArray();
        _ = worldOpacityBuffer.CreateFromImages(new(imgs));


        //mat.SetShaderParameter("world_buffer", worldBuffer);
        RenderingServer.GlobalShaderParameterSet("world_colors", worldColorsBuffer);
        RenderingServer.GlobalShaderParameterSet("world_opacity", worldOpacityBuffer);

        //var center = new Godot.Vector3((int)size.X / 2, (int)size.Y / 2, (int)size.Z / 2);
        //mat.SetShaderParameter("world_center", center);
    }

    //public void FakeUpdateGpuData() {
    //worldBuffer.Update(worldBuffer.GetData());
    //}

    public void UpdateGpuData() {

        var mins = world.Mins;

        for (int itz = 0; itz < size.Z; itz++) {
            Image img = worldColorsBuffer.GetLayerData(itz);
            //Image img = wdata[itz];
            for (int itx = 0; itx < size.X; itx++) {
                for (int ity = 0; ity < size.Y; ity++) {

                    var xyz = mins + new Vector3T<long>(itx, ity, itz);
                    Voxel voxel = world[xyz];
                    bool Opaque = world.Opacity[xyz];
                    var fcol = voxel.color;
                    float lum = (fcol * (0.2126f, 0.7152f, 0.0722f)).Sum();
                    fcol /= 1 + lum;
                    fcol = fcol.Clamp(new(0), new(1));
                    //Vector3T<byte> bcol = fcol.Do((val) => (byte)(val * byte.MaxValue));

                    //uint r = ((bcol.X) & mask8b) << 0; //0-7
                    //uint g = ((bcol.Y) & mask8b) << 8; //8-15
                    //uint b = ((bcol.Z) & mask8b) << 16;//16-23
                    //uint a = ((Opaque ? 1u : 0u) & mask1b) << 24;//24-24
                    //uint u = ((voxel.uv.X) & mask3b) << 26;//26-28
                    //uint v = ((voxel.uv.Y) & mask3b) << 29;//29-32

                    //uint data = r | g | b | a | u | v;

                    Color cdata = new() {
                        R = fcol.X,
                        G = fcol.Y,
                        B = fcol.Z,
                        A = Opaque ? 1 : 0
                    };

                    img.SetPixel(itx, ity, cdata);
                }
            }
            //wdata[itz] = img;
            worldColorsBuffer.UpdateLayer(img, itz);
        }

        //worldBuffer.Update(wdata);
    }

    public void UpdateGpuOpacity() {
        var mins = -world.chunks.Center;

        for (int itz = 0; itz < csize.Z; itz++) {
            Image img = Image.CreateEmpty((int)csize.X, (int)csize.Y, false, Image.Format.Rgf);
            //Image img = worldColorsBuffer.GetLayerData(itz);
            //Image img = wdata[itz];
            for (int itx = 0; itx < csize.X; itx++) {
                for (int ity = 0; ity < csize.Y; ity++) {
                    var xyz = mins + new Vector3T<long>(itx, ity, itz);

                    ulong opacity = world.Opacity.chunks[xyz].Data;

                    uint data1 = (uint)opacity;
                    uint data2 = (uint)(opacity >> 32);

                    Color cdata = new() {
                        R = BitConverter.UInt32BitsToSingle(data1),
                        G = BitConverter.UInt32BitsToSingle(data2)
                    };

                    img.SetPixel(itx, ity, cdata);
                }
            }
            worldOpacityBuffer.UpdateLayer(img, itz);
        }

    }
}
