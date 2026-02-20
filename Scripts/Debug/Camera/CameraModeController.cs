using Godot;
using BiomeArchitectV2.Player;
using BiomeArchitectV2.Terrain.Streaming;

namespace BiomeArchitectV2.Debug.Camera
{
    public sealed partial class CameraModeController : Node
    {
        [Export] private FreeCamCamera2D _freeCam = null!;
        [Export] private Camera2D _playerCam = null!;
        [Export] private PlayerController _player = null!;
        [Export] private TerrainChunkStreamer _terrainStreamer = null!;

        private bool _inspectMode = false;



        public override void _Ready()
        {
            ApplyMode(inspectMode: false);
        }



        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("toggle_debug_mode"))
                ApplyMode(!_inspectMode);
        }



        private void ApplyMode(bool inspectMode)
        {
            _inspectMode = inspectMode;

            if (_inspectMode)
            {
                _freeCam.MakeCurrent();
                _player.ControlEnabled = false;
                _terrainStreamer.SetFollowTarget(_freeCam);

                GD.Print("INSPECT MODE");
            }
            else
            {
                _playerCam.MakeCurrent();
                _player.ControlEnabled = true;
                _terrainStreamer.SetFollowTarget(_player);

                GD.Print("PLAY MODE");
            }
        }
    }
}