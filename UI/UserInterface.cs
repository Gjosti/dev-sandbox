using Godot;
using System;

public partial class UserInterface : Control
{
	[ExportGroup("UI Settings")]
	[Export] public Player Player { get; set; }
	[Export] public PackedScene PauseMenuScene { get; set; }

	private Label _velocityLabel;
	private FPSCounter _fpsCounter;
	private InputManager _inputManager;
	private MarginContainer _marginContainer;

	// Velocity label throttling - only update when velocity changes significantly
	private float _lastDisplayedVelocity = -1f;
	private const float VelocityUpdateThreshold = 0.1f;

	public override void _Ready()
	{
		_velocityLabel = GetNode<Label>("%VelocityLabel");
		_fpsCounter = GetNode<FPSCounter>("%FPSCounter");
		_marginContainer = GetNode<MarginContainer>("MarginContainer");
		_inputManager = GetNode<InputManager>("/root/InputManager");

		if (Player != null)
		{
			Player.VelocityCurrent += OnPlayerVelocityCurrent;
		}

		// Load custom bindings on startup
		_inputManager.LoadCustomBindings();

		// Load FPS counter preference
		bool showFps = ProjectSettings.GetSetting("user/ui/show_fps_counter", false).AsBool();
		SetFPSCounterVisible(showFps);

		// Load UI margin settings (percentage-based)
		float horizontalMarginPercent = (float)ProjectSettings.GetSetting("user/ui/horizontal_margin", 0.0).AsDouble();
		float verticalMarginPercent = (float)ProjectSettings.GetSetting("user/ui/vertical_margin", 0.0).AsDouble();
		SetUIMarginsByPercent(horizontalMarginPercent, verticalMarginPercent);
	}

	private void OnPlayerVelocityCurrent(Vector3 currentVelocity)
	{
		// Only update label if velocity changed significantly to reduce string allocations
		float currentVelocityMagnitude = Mathf.Round(currentVelocity.Length() * 10) / 10;
		if (Mathf.Abs(currentVelocityMagnitude - _lastDisplayedVelocity) >= VelocityUpdateThreshold)
		{
			_velocityLabel.Text = currentVelocityMagnitude.ToString();
			_lastDisplayedVelocity = currentVelocityMagnitude;
		}
	}

	public void SetFPSCounterVisible(bool visible)
	{
		if (_fpsCounter != null)
		{
			_fpsCounter.Visible = visible;
		}
	}

	public void SetUIMargins(int horizontalMargin, int verticalMargin)
	{
		if (_marginContainer != null)
		{
			_marginContainer.AddThemeConstantOverride("margin_left", horizontalMargin);
			_marginContainer.AddThemeConstantOverride("margin_right", horizontalMargin);
			_marginContainer.AddThemeConstantOverride("margin_top", verticalMargin);
			_marginContainer.AddThemeConstantOverride("margin_bottom", verticalMargin);
		}
	}

	public void SetUIMarginsByPercent(float horizontalPercent, float verticalPercent)
	{
		if (_marginContainer == null)
			return;

		Vector2I viewportSize = (Vector2I)GetViewportRect().Size;
		// Calculate pixel margins: horizontal is percentage of width, vertical is percentage of height
		int horizontalMargin = (int)(viewportSize.X * (horizontalPercent / 100.0f));
		int verticalMargin = (int)(viewportSize.Y * (verticalPercent / 100.0f));
		
		SetUIMargins(horizontalMargin, verticalMargin);
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
