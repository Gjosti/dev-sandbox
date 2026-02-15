using Godot;
using System;

public partial class UserInterface : Control
{
	[Export] public Player Player { get; set; }
	[Export] public PackedScene PauseMenuScene { get; set; }

	private Label _velocityLabel;
	private InputManager _inputManager;

	public override void _Ready()
	{
		_velocityLabel = GetNode<Label>("%VelocityLabel");
		_inputManager = GetNode<InputManager>("/root/InputManager");

		if (Player != null)
		{
			Player.VelocityCurrent += OnPlayerVelocityCurrent;
		}

		// Load custom bindings on startup
		_inputManager.LoadCustomBindings();
	}

	public override void _Process(double delta)
	{
	}

	private void OnPlayerVelocityCurrent(Vector3 currentVelocity)
	{
		_velocityLabel.Text = (Mathf.Round(currentVelocity.Length() * 10) / 10).ToString();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Open pause menu with Escape key
		if (@event.IsActionPressed("ui_cancel"))
		{
			// Check if pause menu is already open
			if (GetTree().Root.HasNode("PauseMenu"))
				return;

			// Instantiate the pause menu
			if (PauseMenuScene != null)
			{
				var pauseMenu = PauseMenuScene.Instantiate<Control>();
				GetTree().Root.AddChild(pauseMenu);
				GetViewport().SetInputAsHandled();
			}
			else
			{
				GD.PrintErr("PauseMenuScene not assigned in UserInterface!");
			}
		}
	}
}
