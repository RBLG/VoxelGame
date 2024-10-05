using Godot;
using System;
using System.Linq;
using VoxelGame.scripts.content;

namespace voxelgame.scripts;

[Tool]
public partial class GenerateWorldTextures : EditorScript {

	public static readonly string ocuppath = "res://voxels/world_1_occupancy.tres";
	public static readonly string colspath = "res://voxels/world_1_colors.tres";

	public override void _Run() {
		World world = new();
		world.Generate(new());

		Texture2DArray occupancy = ResourceLoader.Exists(ocuppath) ? GD.Load<Texture2DArray>(ocuppath) : new();
		Image[] imgs = new Image[VoxelEngine.csize.Z].Select((i) => Image.CreateEmpty(VoxelEngine.csize.X, VoxelEngine.csize.Y, false, VoxelEngine.OpacityFormat)).ToArray();
		_ = occupancy.CreateFromImages(new(imgs));
		occupancy.TakeOverPath(ocuppath);
		occupancy.ResourcePath = ocuppath;

		Texture2DArray colors = ResourceLoader.Exists(colspath) ? GD.Load<Texture2DArray>(colspath) : new();
		Image[] imgs2 = new Image[VoxelEngine.size.Z].Select((i) => Image.CreateEmpty(VoxelEngine.size.X, VoxelEngine.size.Y, false, VoxelEngine.ColorFormat)).ToArray();
		_ = colors.CreateFromImages(new(imgs2));
		colors.TakeOverPath(colspath);
		colors.ResourcePath = colspath;


		world.WriteToTexture2DArrays(occupancy, colors);
		_ = ResourceSaver.Save(occupancy);
		_ = ResourceSaver.Save(colors);
		GD.Print("world generated and saved");
	}
}
