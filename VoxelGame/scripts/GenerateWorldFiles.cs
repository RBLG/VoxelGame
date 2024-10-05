using Godot;
using System;
using System.Linq;
using VoxelGame.scripts.content;

namespace voxelgame.scripts;

[Tool]
public partial class GenerateWorldFiles : EditorScript {

    public static readonly string ocuppath = "res://voxels/world_1_occupancy.tres";
    public static readonly string colspath = "res://voxels/world_1_colors.tres";

    public override void _Run() {
        GD.Print("generating world");
        World world = World.Generate(new());

        var csize = VoxelEngine.csize;
        var size = VoxelEngine.size;

        Texture2DArray occupancy = ResourceLoader.Exists(ocuppath) ? GD.Load<Texture2DArray>(ocuppath) : new();
        Image[] imgs = new Image[csize.Z].Select((i) => Image.CreateEmpty(csize.X, csize.Y, false, VoxelEngine.OpacityFormat)).ToArray();
        _ = occupancy.CreateFromImages(new(imgs));
        occupancy.TakeOverPath(ocuppath);
        occupancy.ResourcePath = ocuppath;

        Texture2DArray colors = ResourceLoader.Exists(colspath) ? GD.Load<Texture2DArray>(colspath) : new();
        Image[] imgs2 = new Image[size.Z].Select((i) => Image.CreateEmpty(size.X, size.Y, false, VoxelEngine.ColorFormat)).ToArray();
        _ = colors.CreateFromImages(new(imgs2));
        colors.TakeOverPath(colspath);
        colors.ResourcePath = colspath;


        world.Export(occupancy, colors);
        _ = ResourceSaver.Save(occupancy);
        _ = ResourceSaver.Save(colors);
        GD.Print("world generated and saved");
    }
}
