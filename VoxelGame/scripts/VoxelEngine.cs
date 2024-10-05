using Godot;
using System;
using System.Collections.Concurrent;
using System.Linq;
using VoxelGame.scripts.common;
using VoxelGame.scripts.content;

namespace voxelgame.scripts;

using WorldDataVec3 = FastWorldData<WorldSettings1, Vector3T<float>>;

[Tool]
public partial class VoxelEngine : MeshInstance3D {

    public static readonly Image.Format ColorFormat = Image.Format.Rgb8;
    public static readonly Image.Format OpacityFormat = Image.Format.Rgf;

    public static readonly WorldSettings1 settings = new();

    public uint renderDistance = 64;

    private bool debugNoLight = false;
    private bool debugNoColor = false;
    private bool debugShowSteps = false;

    private Texture2DArray worldColors = new();
    private Texture2DArray worldOccupancy = new();


    private bool updatingLight = false;

    [Export]
    public bool UpdatingLight {
        get => updatingLight;
        set {
            if (updatingLight != value) {
                if (value) {
                    wble = new(world, this);
                    wble.AsyncDoLighting();
                } else if(wble!=null) {
                    wble.Enabled = false;
                    wble.WaitEnd();
                    wble = null;
                }
            }
            updatingLight = value;
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

    [Export]
    public Texture2DArray WorldColors {
        get => worldColors;
        set {
            if (worldColors != null) {
                try {
                    worldColors.Changed -= OnWorldDataChanged;
                } catch (Exception) { }
            }
            if (value == null) { GD.PrintErr("value was null in worldcolor"); }
            value ??= new();

            UpdatingLight = false;

            worldColors = value;
            worldColors.Changed += OnWorldDataChanged;

            worldColorsBuffer = worldColors;
            RenderingServer.GlobalShaderParameterSet("world_colors", worldColorsBuffer);
        }
    }

    [Export]
    public Texture2DArray WorldOccupancy {
        get => worldOccupancy;
        set {
            if (worldOccupancy != null) {
                try {
                    worldOccupancy.Changed -= OnWorldDataChanged;
                } catch (Exception) { }
            }
            if (value == null) { GD.PrintErr("value was null in worldoccup"); }
            value ??= new();
            worldOccupancy = value;
            worldOccupancy.Changed += OnWorldDataChanged;

            worldOpacityBuffer = worldOccupancy;
            RenderingServer.GlobalShaderParameterSet("world_opacity", worldOpacityBuffer);
        }
    }

    public void OnWorldDataChanged() {
        UpdateWorld();
        worldColorsBuffer = worldColors;
        worldOpacityBuffer = worldOccupancy;
    }

    ShaderMaterial mat = GD.Load<ShaderMaterial>("res://shaders/VoxelEngineMaterial.tres");

    public readonly static Vector3T<int> csize = settings.Size;
    public readonly static Vector3T<int> size = csize * settings.ChunkSize;
    //public readonly static Vector3T<int> isize = size;
    public readonly static Vector3T<int> ccenter = csize.Do((v) => v / 2);
    public readonly static Vector3T<int> center = ccenter * settings.ChunkSize;
    private World world = new();

    private BadLightEngine? wble;

    public VoxelEngine() : base() {
        if (!Engine.IsEditorHint()) {
            wble = new(world, this);
        }
        worldOccupancy.Changed += OnWorldDataChanged;
        worldColors.Changed += OnWorldDataChanged;
    }

    public override void _ExitTree() {
        wble?.WaitEnd();
    }

    public override void _Ready() {
        UpdateWorld();
        //world.Generate(new WorldGenerator1());
        if (!Engine.IsEditorHint()) {
            wble!.AsyncDoLighting();
        }

        CreateGpuData();
        UpdateGpuOpacity();
        SendColorLayersUpdates();
    }

    public override void _Process(double delta) {
        base._Process(delta);

        SendColorLayersUpdates();
    }

    private Texture2DArray worldColorsBuffer = new();
    private Texture2DArray worldOpacityBuffer = new();

    public void CreateGpuData() {
        mat.SetShaderParameter("debug_no_light", debugNoLight);
        mat.SetShaderParameter("debug_no_color", debugNoColor);
        mat.SetShaderParameter("debug_show_steps", debugShowSteps);
        RenderingServer.GlobalShaderParameterSet("render_distance", renderDistance);
        RenderingServer.GlobalShaderParameterSet("world_center", new Vector3(center.X, center.Y, center.Z));

        if (worldColorsBuffer.GetWidth() == 0) {
            Image[] imgs = new Image[size.Z].Select((i) => Image.CreateEmpty(size.X, size.Y, false, ColorFormat)).ToArray();
            _ = worldColorsBuffer.CreateFromImages(new(imgs));
        }

        if (worldOpacityBuffer.GetWidth() == 0) {
            Image[] imgs = new Image[csize.Z].Select((i) => Image.CreateEmpty(csize.X, csize.Y, false, OpacityFormat)).ToArray();
            _ = worldOpacityBuffer.CreateFromImages(new(imgs));
        }

        RenderingServer.GlobalShaderParameterSet("world_colors", worldColorsBuffer);
        RenderingServer.GlobalShaderParameterSet("world_opacity", worldOpacityBuffer);

        //var center = new Godot.Vector3((int)size.X / 2, (int)size.Y / 2, (int)size.Z / 2);
        //mat.SetShaderParameter("world_center", center);
    }

    public void SendColorLayersUpdates() {
        bool updated = false;
        while (colorLayerUpdates.TryDequeue(out var update)) {
            worldColorsBuffer.UpdateLayer(update.Layer, update.Index);
            updated = true;
        }
        if (Engine.IsEditorHint() && updated) {
            ResourceSaver.Save(worldColorsBuffer);
        }
    }

    public void UpdateGpuOpacity() {
        var mins = -world.chunks.Chunks.Center;

        for (int itz = 0; itz < csize.Z; itz++) {
            Image img = Image.CreateEmpty(csize.X, csize.Y, false, Image.Format.Rgf);

            for (int itx = 0; itx < csize.X; itx++) {
                for (int ity = 0; ity < csize.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);

                    ulong opacity = world.Opacity.Chunks[xyz].Data;

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

    public readonly ConcurrentQueue<LayerUpdate> colorLayerUpdates = new();


    public void PrepareColorLayers(WorldDataVec3 colors) {
        var mins = world.chunks.Mins;

        for (int itz = 0; itz < size.Z; itz++) {
            Image img = Image.CreateEmpty(size.X, size.Y, false, ColorFormat);

            for (int itx = 0; itx < size.X; itx++) {
                for (int ity = 0; ity < size.Y; ity++) {
                    var xyz = mins + (itx, ity, itz);
                    world.chunks.DeconstructPosToIndex(xyz, out var wind, out var cind);

                    var fcol = colors[wind, cind];
                    float lum = (fcol * (0.2126f, 0.7152f, 0.0722f)).Sum();
                    fcol /= 1 + lum;
                    fcol = fcol.Clamp(new(0), new(1));

                    Color cdata = new() {
                        R = fcol.X,
                        G = fcol.Y,
                        B = fcol.Z,
                    };

                    img.SetPixel(itx, ity, cdata);
                }
            }
            colorLayerUpdates.Enqueue(new(itz, img));
        }
    }

    public void SetGpuOccupancy(Texture2DArray occup) {
        worldOpacityBuffer = occup;
        RenderingServer.GlobalShaderParameterSet("world_opacity", occup);
    }
    public void SetGpuColors(Texture2DArray colors) {
        worldColorsBuffer = colors;
        RenderingServer.GlobalShaderParameterSet("world_colors", colors);
    }

    public void UpdateWorld() {
        UpdatingLight = false;
        world = World.Import(WorldOccupancy, WorldColors);
    }
}

public record LayerUpdate(int Index, Image Layer);