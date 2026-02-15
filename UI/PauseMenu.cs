using Godot;

/// <summary>
/// Pause menu that opens with Escape, pauses the game, and provides access to settings
/// </summary>
public partial class PauseMenu : Control
{
	[Export] public PackedScene SettingsMenuScene { get; set; }

	private Button _resumeButton;
	private Button _settingsButton;
	private Button _quitButton;

	public override void _Ready()
	{
		_resumeButton = GetNode<Button>("%ResumeButton");
		_settingsButton = GetNode<Button>("%SettingsButton");
		_quitButton = GetNode<Button>("%QuitButton");

		_resumeButton.Pressed += OnResumePressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;

		// Show mouse cursor for menu interaction
		Input.MouseMode = Input.MouseModeEnum.Visible;

		// Ensure game is paused when menu appears
		GetTree().Paused = true;
		
		// Set initial focus for keyboard navigation
		_resumeButton.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Close pause menu with Escape (if settings not open)
		if (@event.IsActionPressed("ui_cancel"))
		{
			// Check if settings menu is open
			var settingsNode = GetTree().Root.GetNodeOrNull("SettingsMenu");
			
			if (settingsNode != null)
			{
				// Close settings menu (returns to pause menu)
				settingsNode.QueueFree();
				GetViewport().SetInputAsHandled();
			}
			else
			{
				// Close pause menu and resume game
				OnResumePressed();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void OnResumePressed()
	{
		// Capture mouse again for gameplay
		Input.MouseMode = Input.MouseModeEnum.Captured;
		GetTree().Paused = false;
		QueueFree();
	}

	private void OnSettingsPressed()
	{
		// Check if settings menu is already open
		if (GetTree().Root.HasNode("SettingsMenu"))
			return;

		// Instantiate the settings menu
		if (SettingsMenuScene != null)
		{
			var settingsUI = SettingsMenuScene.Instantiate<Control>();
			// Set the settings UI to also work in pause mode
			settingsUI.ProcessMode = ProcessModeEnum.Always;
			GetTree().Root.AddChild(settingsUI);
		}
		else
		{
			GD.PrintErr("SettingsMenuScene not assigned in PauseMenu!");
		}
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	public override void _ExitTree()
	{
		// Capture mouse again when menu is removed
		Input.MouseMode = Input.MouseModeEnum.Captured;
		
		// Make sure game is unpaused when menu is removed
		if (GetTree().Paused)
		{
			GetTree().Paused = false;
		}
	}
}
