using Godot;
using System;
using VoxelGame.scripts.common;
using VoxelGame.scripts.content;

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
	}

	public override void _Process(double delta) {
		base._Process(delta);

		if (wble.ApplyLatestResults()) {
			UpdateGpuData();
		}
	}

	private ImageTexture3D worldBuffer = new();

	public void CreateGpuData() {


		Image.Format format = Image.Format.Rf;

		Image[] imgs = new Image[size.Z];
		for (uint itz = 0; itz < size.Z; itz++) {
			Image img = Image.Create(isize.X, isize.Y, false, format);
			img.Fill(new());
			imgs[itz] = img;
		}
		worldBuffer.Create(format, isize.X, isize.Y, isize.Z, false, new(imgs));

		mat.SetShaderParameter("world_buffer", worldBuffer);

        //var center = new Godot.Vector3((int)size.X / 2, (int)size.Y / 2, (int)size.Z / 2);
        //mat.SetShaderParameter("world_center", center);
    }
	public void FakeUpdateGpuData() {
        worldBuffer.Update(worldBuffer.GetData());
    }
	public void UpdateGpuData() {

		Godot.Collections.Array<Image> wdata = worldBuffer.GetData();
		var mins = world.Mins;

		for (int itz = 0; itz < size.Z; itz++) {
			Image img = wdata[itz];
			for (int itx = 0; itx < size.X; itx++) {
				for (int ity = 0; ity < size.Y; ity++) {

					var xyz = mins + new Vector3T<long>(itx, ity, itz);
					Voxel voxel = world[xyz];
					bool Opaque = world.Opacity[xyz];
					Vector3T<byte> bcol = voxel.color.Clamp(new(0), new(1)).Do((val) => (byte)(val * byte.MaxValue));

					uint r = ((bcol.X) & mask8b) << 0; //0-7
					uint g = ((bcol.Y) & mask8b) << 8; //8-15
					uint b = ((bcol.Z) & mask8b) << 16;//16-23
					uint a = ((Opaque ? 1u : 0u) & mask1b) << 24;//24-24
					uint u = ((voxel.uv.X) & mask3b) << 26;//26-28
					uint v = ((voxel.uv.Y) & mask3b) << 29;//29-32

					uint data = r | g | b | a | u | v;

					Color cdata = new() {
						R = BitConverter.UInt32BitsToSingle(data),
						B = 0,
						G = 0
					};

					wdata[itz].SetPixel(itx, ity, cdata);
				}
			}
			wdata[itz] = img;
		}

		worldBuffer.Update(wdata);
	}
}
