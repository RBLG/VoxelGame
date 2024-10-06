using Godot;
using System;
using System.Collections.Concurrent;
using VoxelGame.scripts.common;
using VoxelGame.scripts.content;

namespace voxelgame.scripts;

using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;

[Tool]
public partial class VoxelEngine : MeshInstance3D {
    public static readonly Image.Format ColorFormat = Image.Format.Rgb8;
    public static readonly Image.Format OccupancyFormat = Image.Format.Rgf;

    public static readonly WorldSettings1 settings = new();
    public static readonly Vector3T<int> csize = settings.GridSize;
    public static readonly Vector3T<int> size = csize * settings.ChunkSize;
    public static readonly Vector3T<int> ccenter = csize / 2;
    public static readonly Vector3T<int> center = ccenter * settings.ChunkSize;

    public uint renderDistance = 64;

    private bool debugNoLight = false;
    private bool debugNoColor = false;
    private bool debugShowSteps = false;

    private ShaderMaterial mat = GD.Load<ShaderMaterial>("res://shaders/VoxelEngineMaterial.tres");
    private Texture2DArray worldColors = GD.Load<Texture2DArray>("res://voxels/world_1_colors.tres");
    private Texture2DArray worldOccupancy = GD.Load<Texture2DArray>("res://voxels/world_1_occupancy.tres");

    private World world;
    private BadLightEngine? wble;

    private Texture2DArray worldColorsBuffer = GdHelper.NewBlankTexture2DArray(size, false, ColorFormat);
    private Texture2DArray worldOccupancyBuffer = GdHelper.NewBlankTexture2DArray(csize, false, OccupancyFormat);
    public VoxelEngine() : base() {
        BakingLight = false;
        world = World.Import(worldOccupancy, worldColors);

        if (!Engine.IsEditorHint()) {
            wble = new(world, this);
            wble.AsyncStartLighting();
        }
    }

    public override void _Ready() {
        worldOccupancy.Changed += UpdateWorld;
        worldColors.Changed += UpdateWorld;

        InitShaderSettings();
        UpdateGpuOccupancy();
        UpdateGpuColors();
    }

    public override void _ExitTree() {
        wble?.Stop();
    }

    public override void _Process(double delta) {
        SendColorLayersUpdates();
    }

    public void UpdateWorld() {
        BakingLight = false;
        world = World.Import(worldOccupancy, worldColors);

        UpdateGpuOccupancy();
        UpdateGpuColors();
    }

    public void InitShaderSettings() {
        mat.SetShaderParameter("debug_no_light", debugNoLight);
        mat.SetShaderParameter("debug_no_color", debugNoColor);
        mat.SetShaderParameter("debug_show_steps", debugShowSteps);
        RenderingServer.GlobalShaderParameterSet("render_distance", renderDistance);
        RenderingServer.GlobalShaderParameterSet("world_center", new Vector3(center.X, center.Y, center.Z));

        RenderingServer.GlobalShaderParameterSet("world_colors", worldColorsBuffer);
        RenderingServer.GlobalShaderParameterSet("world_opacity", worldOccupancyBuffer);
    }

    public void UpdateGpuOccupancy() {
        var mins = -world.Occupancy.Chunks.Center;
        var csize = world.Occupancy.Chunks.Size;

        for (int itz = 0; itz < csize.Z; itz++) {
            Image img = Image.CreateEmpty(csize.X, csize.Y, false, Image.Format.Rgf);

            for (int itx = 0; itx < csize.X; itx++) {
                for (int ity = 0; ity < csize.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

                    ulong opacity = world.Occupancy.Chunks[xyz].Data;

                    uint data1 = (uint)opacity;
                    uint data2 = (uint)(opacity >> 32);

                    Color cdata = new() {
                        R = BitConverter.UInt32BitsToSingle(data1),
                        G = BitConverter.UInt32BitsToSingle(data2)
                    };

                    img.SetPixel(itx, ity, cdata);
                }
            }
            worldOccupancyBuffer.UpdateLayer(img, itz);
        }

    }

    public void UpdateGpuColors() {
        WorldDataVec3 colors = new((wind, cind) => world.Voxels[wind, cind].color);
        PrepareColorLayers(colors);
        SendColorLayersUpdates();
    }

    public readonly ConcurrentQueue<LayerUpdate> colorLayerUpdates = new();

    public void SendColorLayersUpdates() {
        //bool updated = false;
        while (colorLayerUpdates.TryDequeue(out var update)) {
            worldColorsBuffer.UpdateLayer(update.Layer, update.Index);

            //if (Engine.IsEditorHint() && BakingLight) {
            //    worldColors?.UpdateLayer(update.Layer, update.Index);
            //}
            //updated = true;
        }
        //if (Engine.IsEditorHint() && BakingLight && updated) {
        //    ResourceSaver.Save(worldColors);
        //}
    }

    public void PrepareColorLayers(WorldDataVec3 colors) {
        var mins = colors.Settings.TotalMins;
        var size = colors.Settings.TotalSize;

        int count = 0;
        for (int itz = 0; itz < size.Z; itz++) {
            Image img = Image.CreateEmpty(size.X, size.Y, false, ColorFormat);

            for (int itx = 0; itx < size.X; itx++) {
                for (int ity = 0; ity < size.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);
                    colors.DeconstructPosToIndex(xyz, out var wind, out var cind);

                    var fcol = colors[wind, cind];
                    float lum = (fcol * (0.2126f, 0.7152f, 0.0722f)).Sum();
                    fcol /= 1 + lum;
                    fcol = fcol.Clamp(new(0), new(1));

                    //Color cdata = new() {R = fcol.X,G = fcol.Y,B = fcol.Z,};
                    img.SetPixel(itx, ity, new(fcol.X, fcol.Y, fcol.Z));
                }
            }
            count++;
            colorLayerUpdates.Enqueue(new(itz, img));
        }
        GD.Print($"queued {count} layer updates");
    }

    [Export]
    public bool UseBakedLight { get; set; } = false;

    [Export]
    public bool BakingLight {
        get => wble != null;
        set {
            if (!IsNodeReady()) { return; }
            if (value && wble == null) {
                wble = new(world, this);
                wble.AsyncStartLighting();
            } else if (!value && wble != null) {
                wble.Stop();
                wble = null;
            }
        }
    }

    [Export]
    public uint RenderDistance {
        get => renderDistance;
        set => RenderingServer.GlobalShaderParameterSet("render_distance", renderDistance = value);
    }
    [Export]
    public bool DebugNoLight {
        get => debugNoLight;
        set => mat.SetShaderParameter("debug_no_light", debugNoLight = value);

    }
    [Export]
    public bool DebugNoColor {
        get => debugNoColor;
        set => mat.SetShaderParameter("debug_no_color", debugNoColor = value);

    }
    [Export]
    public bool DebugShowSteps {
        get => debugShowSteps;
        set => mat.SetShaderParameter("debug_show_steps", debugShowSteps = value);
    }
}

public record LayerUpdate(int Index, Image Layer);
