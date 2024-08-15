using Godot;
using System;

namespace VoxelGame.scripts;
public partial class Player : CharacterBody3D {
    Camera3D? cam;

    public override void _Ready() {
        cam = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public float Speed = 5f;
    public float RunSpeedBuff = 1f;

    public override void _PhysicsProcess(double delta) {
        float x1 = Input.GetActionStrength("Right");
        float x2 = Input.GetActionStrength("Left");
        float y1 = Input.GetActionStrength("Up");
        float y2 = Input.GetActionStrength("Down");
        float z1 = Input.GetActionStrength("Forward");
        float z2 = Input.GetActionStrength("Backward");
        float run = Input.GetActionStrength("Run");

        Basis basis = cam!.GlobalTransform.Basis;
        Vector3 forwardv = -basis.Z;
        Vector3 leftvec_ = basis.X;
        Vector3 upvec___ = basis.Y;

        float x = x1 - x2;
        float y = y1 - y2;
        float z = z1 - z2;

        Velocity = (forwardv * z + leftvec_ * x + upvec___ * y).Normalized() * Speed * (1 + run * RunSpeedBuff);

        MoveAndSlide();
    }


    float mouse_sens = 0.25f;
    float camera_anglev = 0;


    bool captured = true;
    public override void _Input(InputEvent @event) {
        if (captured && @event is InputEventMouseMotion event2) {
            var changev = -event2.Relative.Y * mouse_sens;
            float last_angle = camera_anglev;
            camera_anglev += changev;
            camera_anglev = Mathf.Clamp(camera_anglev, -90, 90);
            changev = camera_anglev - last_angle;
            cam!.Rotation += new Vector3(
                Godot.Mathf.DegToRad(changev),
                Godot.Mathf.DegToRad(-event2.Relative.X * mouse_sens),
                0
            );
            return;
        }

        if (@event.IsActionPressed("Exit")) {
            if (captured == true) {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                captured = false;
            } else {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                captured = true;
            }
            return;
        }
    }
}
