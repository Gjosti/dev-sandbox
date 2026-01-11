using Godot;
using System;

public partial class UserInterface : Control
{
    [Export] public Player Player { get; set; }
    
    private Label _velocityLabel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _velocityLabel = GetNode<Label>("%VelocityLabel");
        
        if (Player != null)
        {
            Player.VelocityCurrent += OnPlayerVelocityCurrent;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    private void OnPlayerVelocityCurrent(Vector3 currentVelocity)
    {
        _velocityLabel.Text = (Mathf.Round(currentVelocity.Length() * 10) / 10).ToString();
    }
}
