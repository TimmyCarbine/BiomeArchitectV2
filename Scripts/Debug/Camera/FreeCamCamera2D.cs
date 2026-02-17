using Godot;

namespace BiomeArchitectV2.Debug.Camera
{
    public sealed partial class FreeCamCamera2D : Camera2D
    {
        private const float BASE_MOVE_SPEED_PX_PER_SEC = 4000f;
        private const float FAST_MOVE_MULTIPLIER = 10.0f;
        private const float SLOW_MOVE_MULTIPLIER = 0.25f;
        private const float ZOOM_STEP = 0.12f;
        private const float ZOOM_STEP_FAST  = 0.75f;
        private const float ZOOM_STEP_FINE  = 0.04f;
        private const float MIN_ZOOM  = 0.01f;
        private const float MAX_ZOOM  = 5f;
        private const bool ENABLE_MIDDLE_MOUSE_DRAG  = true;

        private bool _isDragging;
        private Vector2 _dragStartMouseScreen;
        private Vector2 _dragStartCameraPos;

        public bool ControlEnabled { get; set; } = true;




        public override void _Ready()
        {
            MakeCurrent();
            GetTree().Root.GrabFocus();
        }



        public override void _Process(double delta)
        {
            if (!ControlEnabled)
                return;
            
            Vector2 dir = Vector2.Zero;

            if (Input.IsActionPressed("freecam_left")) dir.X -= 1f;
            if (Input.IsActionPressed("freecam_right")) dir.X += 1f;
            if (Input.IsActionPressed("freecam_up")) dir.Y -= 1f;
            if (Input.IsActionPressed("freecam_down")) dir.Y += 1f;

            if (dir == Vector2.Zero) return;

            dir = dir.Normalized();
            float speed = BASE_MOVE_SPEED_PX_PER_SEC * GetMoveSpeedMultiplier();
            Position += dir * speed * (float)delta;
        }




        public override void _UnhandledInput(InputEvent e)
        {
            if (ENABLE_MIDDLE_MOUSE_DRAG)
            {
                if (e.IsActionPressed("freecam_drag"))
                {
                    _isDragging = true;
                    _dragStartMouseScreen = GetViewport().GetMousePosition();
                    _dragStartCameraPos = Position;
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (e.IsActionReleased("freecam_drag"))
                {
                    _isDragging = false;
                    GetViewport().SetInputAsHandled();
                    return;
                }

                if (_isDragging && e is InputEventMouseMotion)
                {
                    Vector2 mouseNow = GetViewport().GetMousePosition();
                    Vector2 screenDelta = mouseNow - _dragStartMouseScreen;
                    Vector2 worldDelta = screenDelta * Zoom;
                    Position = _dragStartCameraPos - worldDelta;
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }

            if (e.IsActionPressed("freecam_zoom_in"))
            {
                ApplyZoom(zoomIn: true);
                GetViewport().SetInputAsHandled();
                return;
            }
            else if (e.IsActionPressed("freecam_zoom_out"))
            {
                ApplyZoom(zoomIn: false);
                GetViewport().SetInputAsHandled();
                return;
            }  
        }



        private float GetMoveSpeedMultiplier()
        {
            bool fast = Input.IsActionPressed("freecam_fast");
            bool slow = Input.IsActionPressed("freecam_slow");

            if (fast && slow) return 1f;
            if (fast) return FAST_MOVE_MULTIPLIER;
            if (slow) return SLOW_MOVE_MULTIPLIER;

            return 1f;
        }




        private float GetZoomStep()
        {
            bool fast = Input.IsActionPressed("freecam_fast");
            bool slow = Input.IsActionPressed("freecam_slow");
            if (fast && slow) return ZOOM_STEP;
            if (fast) return ZOOM_STEP_FAST;
            if (slow) return ZOOM_STEP_FINE;
            return ZOOM_STEP;
        }




        private void ApplyZoom(bool zoomIn)
        {
            float step = GetZoomStep();
            float factor = zoomIn ? (1f - step) : (1f + step);
            float next = Mathf.Clamp(Zoom.X * factor, MIN_ZOOM, MAX_ZOOM);
            Zoom = new Vector2(next, next);
        }
    }
}