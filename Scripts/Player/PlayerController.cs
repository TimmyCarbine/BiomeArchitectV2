using Godot;

namespace BiomeArchitectV2.Player
{
    public sealed partial class PlayerController : CharacterBody2D
    {
        [Export] public float MoveSpeed { get; set; } = 180f;
        [Export] public float JumpVelocity { get; set; } = -420f;
        [Export] public float Gravity { get; set; } = 1200f;

        [Export] private Sprite2D _sprite = null!;

        public bool ControlEnabled { get; set; } = false;



        public override void _PhysicsProcess(double delta)
        {
            if (!ControlEnabled)
            {
                Velocity = Vector2.Zero;
                return;
            }

            float dt = (float)delta;

            if (!IsOnFloor())
                Velocity = new Vector2(Velocity.X, Velocity.Y + Gravity * dt);

            float input = 0f;
            if (Input.IsActionPressed("move_left")) input -= 1f;                
            if (Input.IsActionPressed("move_right")) input += 1f;

            if (input < 0f) _sprite.FlipH = false;
            if (input > 0f) _sprite.FlipH = true;

            Velocity = new Vector2(input * MoveSpeed, Velocity.Y);

            if (IsOnFloor() && Input.IsActionJustPressed("jump"))
                Velocity = new Vector2(Velocity.X, JumpVelocity);

            MoveAndSlide();
        }
    }
}