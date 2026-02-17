using Godot;
using BiomeArchitectV2.Player;

namespace BiomeArchitectV2.Debug.Camera
{
    public sealed partial class CameraModeController : Node
    {
        [Export] private FreeCamCamera2D _freeCam = null!;
        [Export] private Camera2D _playerCam = null!;
        [Export] private PlayerController _player = null!;

        private bool _usingPlayerCam = false;



        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_camera_mode"))
                ToggleCamera();

            if (@event.IsActionPressed("toggle_player_control"))
                TogglePlayerControl();
        }



        private void ToggleCamera()
        {
            _usingPlayerCam = !_usingPlayerCam;

            if (_usingPlayerCam)
            {
                _playerCam.MakeCurrent();
                _player.ControlEnabled = true;
            }
            else
                _freeCam.MakeCurrent();
        }



        private void TogglePlayerControl()
        {
            _player.ControlEnabled = !_player.ControlEnabled;
            _freeCam.ControlEnabled = !_player.ControlEnabled;
        }
    }
}