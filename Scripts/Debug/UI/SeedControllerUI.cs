using Godot;
using System;

namespace BiomeArchitectV2.Debug.UI
{
    public sealed partial class SeedControllerUI : CanvasLayer
    {
        [Export] private LineEdit SeedEdit { get; set; } = null!;
        [Export] private Button RandomiseButton { get; set; } = null!;
        [Export] private Button RegenerateButton { get; set;} = null!;
        [Export] private Node RegenTarget { get; set; } = null!;

        private const string REGEN_METHOD = "RegenerateWithSeed";



        public override void _Ready()
        {
            ArgumentNullException.ThrowIfNull(SeedEdit);
            ArgumentNullException.ThrowIfNull(RandomiseButton);
            ArgumentNullException.ThrowIfNull(RegenerateButton);

            RandomiseButton.Pressed += OnRandomisePressed;
            RegenerateButton.Pressed += OnRegeneratePressed;
            SeedEdit.TextSubmitted += _ => OnRegeneratePressed();
        }



        public void Init(Node regenTarget, int initalSeed)
        {
            RegenTarget = regenTarget;
            if (SeedEdit != null) SeedEdit.Text = initalSeed.ToString();
        }



        public void SetSeedText(int seed)
        {
            if (SeedEdit != null) SeedEdit.Text = seed.ToString();
        }



        private void OnRandomisePressed()
        {
            int seed = Random.Shared.Next(int.MinValue, int.MaxValue);
            SetSeedText(seed);
            CallRegen(seed);
        }



        private void OnRegeneratePressed()
        {
            if (SeedEdit == null) return;

            if (!int.TryParse(SeedEdit.Text, out int seed))
            {
                SeedEdit.SelectAll();
                SeedEdit.GrabFocus();
                GD.Print("[BiomeArchitectV2] Invalid seed input.");
                return;
            }
            CallRegen(seed);
        }



        private void CallRegen(int seed)
        {
            if (RegenTarget == null)
            {
                GD.PrintErr($"[BiomeArchitectV2] SeedControllerUI has no RegenTarget assigned.");
                return;
            }

            if (!RegenTarget.HasMethod(REGEN_METHOD))
            {
                GD.PrintErr($"[BiomeArchitectV2] RegenTarget '{RegenTarget.Name}' is missing method {REGEN_METHOD}(int).");
                return;
            }
            RegenTarget.Call(REGEN_METHOD, seed);
        }
    }
}