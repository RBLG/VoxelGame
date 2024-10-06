using Godot;
using VoxelGame.scripts.common;
using VoxelGame.scripts.content;

namespace voxelgame.scripts.runnable;

[Tool]
public partial class GenerateWorldFiles : EditorScript {
    public static readonly string ocuppath = "res://voxels/world_1_occupancy.tres";
    public static readonly string colspath = "res://voxels/world_1_colors.tres";

    public override void _Run() {
        GD.Print("creating/overriding files");
        Texture2DArray occupancy = ResourceLoader.Exists(ocuppath) ? GD.Load<Texture2DArray>(ocuppath) : new();
        GdHelper.CleanTexture(occupancy, VoxelEngine.csize, false, VoxelEngine.OccupancyFormat);
        occupancy.TakeOverPath(ocuppath);
        occupancy.ResourcePath = ocuppath;

        Texture2DArray colors = ResourceLoader.Exists(colspath) ? GD.Load<Texture2DArray>(colspath) : new();
        GdHelper.CleanTexture(colors, VoxelEngine.size, false, VoxelEngine.ColorFormat);
        colors.TakeOverPath(colspath);
        colors.ResourcePath = colspath;

        GD.Print("generating world");
        World world = World.Generate(new());

        GD.Print("exporting world");
        world.Export(occupancy, colors);
        var errocc = ResourceSaver.Save(occupancy);
        var errcol = ResourceSaver.Save(colors);
        if (errocc == Error.Ok && errcol == Error.Ok) {
            GD.Print("world saved");
        } else {
            GD.PrintErr(errcol, errocc);
        }
    }
}
