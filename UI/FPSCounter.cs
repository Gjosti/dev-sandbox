using Godot;

/// <summary>
/// Simple FPS counter display in the top-left corner, toggleable from settings
/// </summary>
public partial class FPSCounter : Label
{
	private float _updateTimer = 0.5f;
	private float _updateInterval = 0.5f;

	public override void _Ready()
	{
		// Initially hide the FPS counter
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;

		_updateTimer -= (float)delta;
		if (_updateTimer <= 0)
		{
			Text = $"FPS: {Engine.GetFramesPerSecond()}";
			_updateTimer = _updateInterval;
		}
	}
}
