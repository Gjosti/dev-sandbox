using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main settings menu with multiple categories: Gameplay, Video, Sound, Keybindings
/// </summary>
public partial class SettingsMenu : Control
{
	private InputManager _inputManager;
	private Button _gameplayButton;
	private Button _videoButton;
	private Button _soundButton;
	private Button _keybindingsButton;
	private Button _backButton;

	private Control _gameplayPanel;
	private Control _videoPanel;
	private Control _soundPanel;
	private Control _keybindingsPanel;

	// Video settings controls
	private OptionButton _resolutionOption;
	private OptionButton _windowModeOption;
	private CheckButton _vsyncCheckButton;
	private Button _applyVideoButton;

	// Keybindings controls
	private VBoxContainer _bindingsContainer;
	private Button _resetAllBindingsButton;
	private Button _currentlyRebindingButton;
	private string _currentlyRebindingAction;
	private int _currentlyRebindingIndex;

	// Resolution dictionary with aspect ratio labels
	private Dictionary<string, (Vector2I size, string aspectRatio)> _resolutions = new Dictionary<string, (Vector2I, string)>
	{
		// 16:9 (Most common)
		{ "3840x2160", (new Vector2I(3840, 2160), "16:9") },
		{ "2560x1440", (new Vector2I(2560, 1440), "16:9") },
		{ "1920x1080", (new Vector2I(1920, 1080), "16:9") },
		{ "1600x900", (new Vector2I(1600, 900), "16:9") },
		{ "1366x768", (new Vector2I(1366, 768), "16:9") },
		{ "1280x720", (new Vector2I(1280, 720), "16:9") },
		
		// 16:10 (Laptops, professional displays)
		{ "2560x1600", (new Vector2I(2560, 1600), "16:10") },
		{ "1920x1200", (new Vector2I(1920, 1200), "16:10") },
		{ "1680x1050", (new Vector2I(1680, 1050), "16:10") },
		{ "1440x900", (new Vector2I(1440, 900), "16:10") },
		{ "1280x800", (new Vector2I(1280, 800), "16:10") },
		
		// 21:9 (Ultrawide)
		{ "3440x1440", (new Vector2I(3440, 1440), "21:9") },
		{ "2560x1080", (new Vector2I(2560, 1080), "21:9") },
		
		// 32:9 (Super ultrawide)
		{ "5120x1440", (new Vector2I(5120, 1440), "32:9") },
		{ "3840x1080", (new Vector2I(3840, 1080), "32:9") }
	};

	public override void _Ready()
	{
		_inputManager = GetNode<InputManager>("/root/InputManager");
		
		// Get category buttons
		_gameplayButton = GetNode<Button>("%GameplayButton");
		_videoButton = GetNode<Button>("%VideoButton");
		_soundButton = GetNode<Button>("%SoundButton");
		_keybindingsButton = GetNode<Button>("%KeybindingsButton");
		_backButton = GetNode<Button>("%BackButton");

		// Get panels
		_gameplayPanel = GetNode<Control>("%GameplayPanel");
		_videoPanel = GetNode<Control>("%VideoPanel");
		_soundPanel = GetNode<Control>("%SoundPanel");
		_keybindingsPanel = GetNode<Control>("%KeybindingsPanel");

		// Video settings controls
		_resolutionOption = GetNode<OptionButton>("%ResolutionOption");
		_windowModeOption = GetNode<OptionButton>("%WindowModeOption");
		_vsyncCheckButton = GetNode<CheckButton>("%VsyncCheckButton");
		_applyVideoButton = GetNode<Button>("%ApplyVideoButton");

		// Keybindings controls
		_bindingsContainer = GetNode<VBoxContainer>("%BindingsContainer");
		_resetAllBindingsButton = GetNode<Button>("%ResetAllBindingsButton");

		// Connect signals
		_gameplayButton.Pressed += () => ShowPanel("gameplay");
		_videoButton.Pressed += () => ShowPanel("video");
		_soundButton.Pressed += () => ShowPanel("sound");
		_keybindingsButton.Pressed += () => ShowPanel("keybindings");
		_backButton.Pressed += OnBackPressed;
		_applyVideoButton.Pressed += OnApplyVideoSettings;
		_resetAllBindingsButton.Pressed += OnResetAllBindings;

		// Populate video options
		PopulateVideoOptions();
		LoadCurrentVideoSettings();
		
		// Populate keybindings
		PopulateBindings();

		// Show gameplay by default
		ShowPanel("gameplay");
		
		// Set initial focus for keyboard navigation
		_gameplayButton.GrabFocus();
		
		// Disable input processing by default (only enable when rebinding)
		SetProcessInput(false);
	}

	private void PopulateVideoOptions()
	{
		// Add resolutions sorted by pixel count (descending - largest first)
		var sortedResolutions = _resolutions
			.OrderByDescending(kvp => kvp.Value.size.X * kvp.Value.size.Y)
			.ToList();
		
		foreach (var kvp in sortedResolutions)
		{
			string displayText = $"{kvp.Key} ({kvp.Value.aspectRatio})";
			_resolutionOption.AddItem(displayText);
		}

		// Add window modes
		_windowModeOption.AddItem("Fullscreen");
		_windowModeOption.AddItem("Windowed");
		_windowModeOption.AddItem("Borderless Fullscreen");
	}

	private void LoadCurrentVideoSettings()
	{
		var window = GetWindow();
		
		// Set current resolution
		Vector2I currentSize = window.Size;
		int resIndex = 0;
		int i = 0;
		
		// Sort resolutions the same way as PopulateVideoOptions
		var sortedResolutions = _resolutions
			.OrderByDescending(kvp => kvp.Value.size.X * kvp.Value.size.Y)
			.ToList();
		
		// Find exact match first
		bool foundExactMatch = false;
		foreach (var kvp in sortedResolutions)
		{
			if (kvp.Value.size == currentSize)
			{
				resIndex = i;
				foundExactMatch = true;
				break;
			}
			i++;
		}
		
		// If no exact match (window was resized/clamped), find closest match
		if (!foundExactMatch)
		{
			i = 0;
			int closestIndex = 0;
			int smallestDiff = int.MaxValue;
			
			foreach (var kvp in sortedResolutions)
			{
				int diff = Mathf.Abs((kvp.Value.size.X * kvp.Value.size.Y) - (currentSize.X * currentSize.Y));
				if (diff < smallestDiff)
				{
					smallestDiff = diff;
					closestIndex = i;
				}
				i++;
			}
			resIndex = closestIndex;
		}
		
		_resolutionOption.Selected = resIndex;

		// Set window mode
		if (window.Mode == Window.ModeEnum.Fullscreen)
		{
			_windowModeOption.Selected = 0; // Fullscreen
		}
		else if (window.Mode == Window.ModeEnum.Windowed && window.Borderless)
		{
			_windowModeOption.Selected = 2; // Borderless Fullscreen
		}
		else if (window.Mode == Window.ModeEnum.Windowed)
		{
			_windowModeOption.Selected = 1; // Windowed
		}

		// Set VSync
		_vsyncCheckButton.ButtonPressed = DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Enabled;
	}

	private void OnApplyVideoSettings()
	{
		var window = GetWindow();
		int currentScreen = DisplayServer.WindowGetCurrentScreen();
		Rect2I screenRect = DisplayServer.ScreenGetUsableRect(currentScreen);

		// Get selected resolution
		string selectedText = _resolutionOption.GetItemText(_resolutionOption.Selected);
		string selectedResolution = selectedText.Split(' ')[0];
		
		// Handle different window modes
		switch (_windowModeOption.Selected)
		{
			case 0: // Fullscreen
				window.Mode = Window.ModeEnum.Fullscreen;
				window.Borderless = false;
				
				// Disable content scaling for fullscreen (native resolution)
				window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
				
				// Apply selected resolution for fullscreen
				if (_resolutions.ContainsKey(selectedResolution))
				{
					window.Size = _resolutions[selectedResolution].size;
				}
				break;
				
			case 1: // Windowed
				window.Mode = Window.ModeEnum.Windowed;
				window.Borderless = false;
				
				// Disable content scaling for windowed mode (native resolution)
				window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
				
				// Apply selected resolution for windowed mode
				if (_resolutions.ContainsKey(selectedResolution))
				{
					Vector2I newSize = _resolutions[selectedResolution].size;
					
					// Clamp window size to screen size (with margin for window chrome)
					newSize.X = Mathf.Min(newSize.X, screenRect.Size.X - 20);
					newSize.Y = Mathf.Min(newSize.Y, screenRect.Size.Y - 60);
					
					window.Size = newSize;
					
					// Center the window on the current screen
					Vector2I centerPos = new Vector2I(
						screenRect.Position.X + (screenRect.Size.X - newSize.X) / 2,
						screenRect.Position.Y + (screenRect.Size.Y - newSize.Y) / 2
					);
					window.Position = centerPos;
				}
				break;
				
			case 2: // Borderless Fullscreen
				window.Mode = Window.ModeEnum.Windowed;
				window.Borderless = true;
				
				// Window covers entire screen at native resolution
				Vector2I screenSize = DisplayServer.ScreenGetSize(currentScreen);
				window.Size = screenSize;
				window.Position = DisplayServer.ScreenGetPosition(currentScreen);
				
				// Set viewport to selected resolution (game renders at this resolution)
				if (_resolutions.ContainsKey(selectedResolution))
				{
					Vector2I targetResolution = _resolutions[selectedResolution].size;
					GetWindow().ContentScaleSize = targetResolution;
					GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
					GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
				}
				break;
		}

		// Apply VSync
		DisplayServer.WindowSetVsyncMode(
			_vsyncCheckButton.ButtonPressed ? 
			DisplayServer.VSyncMode.Enabled : 
			DisplayServer.VSyncMode.Disabled
		);
	}

	private void PopulateBindings()
	{
		// Clear existing
		foreach (Node child in _bindingsContainer.GetChildren())
		{
			child.QueueFree();
		}
		
		var actions = _inputManager.GetAllActions();
		
		// Filter to only game actions (skip ui_ actions)
		var gameActions = new System.Collections.Generic.List<string>();
		foreach (string action in actions)
		{
			if (!action.ToString().StartsWith("ui_"))
			{
				gameActions.Add(action);
			}
		}
		
		// Sort alphabetically
		gameActions.Sort();
		
		foreach (string actionName in gameActions)
		{
			AddActionRow(actionName);
		}
	}

	private void AddActionRow(string actionName)
	{
		// Create row container
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 10);
		
		// Action label
		var label = new Label();
		label.Text = FormatActionName(actionName);
		label.CustomMinimumSize = new Vector2(150, 0);
		row.AddChild(label);
		
		// Get current bindings
		var events = _inputManager.GetActionEvents(actionName);
		
		// Primary binding button
		var primaryButton = CreateBindingButton(actionName, 0, 
			events.Count > 0 ? events[0] : null);
		row.AddChild(primaryButton);
		
		// Optional: Secondary binding
		if (events.Count > 1)
		{
			var secondaryButton = CreateBindingButton(actionName, 1, events[1]);
			row.AddChild(secondaryButton);
		}
		else
		{
			// Placeholder for secondary
			var addButton = new Button();
			addButton.Text = "+ Add Alt";
			addButton.CustomMinimumSize = new Vector2(120, 0);
			addButton.Pressed += () => StartRebinding(actionName, 1, addButton);
			row.AddChild(addButton);
		}
		
		// Reset individual action button
		var resetButton = new Button();
		resetButton.Text = "Reset";
		resetButton.Pressed += () => OnResetActionPressed(actionName);
		row.AddChild(resetButton);
		
		_bindingsContainer.AddChild(row);
	}

	private Button CreateBindingButton(string actionName, int index, InputEvent evt)
	{
		var button = new Button();
		button.CustomMinimumSize = new Vector2(150, 0);
		
		if (evt != null)
		{
			button.Text = InputManager.GetEventDisplayString(evt);
		}
		else
		{
			button.Text = "Not Bound";
		}
		
		button.Pressed += () => StartRebinding(actionName, index, button);
		
		return button;
	}

	private void StartRebinding(string actionName, int index, Button button)
	{
		// Cancel previous rebinding if any
		if (_currentlyRebindingButton != null)
		{
			RestoreButtonText(_currentlyRebindingButton, _currentlyRebindingAction, _currentlyRebindingIndex);
		}
		
		_currentlyRebindingButton = button;
		_currentlyRebindingAction = actionName;
		_currentlyRebindingIndex = index;
		
		button.Text = "Press any key...";
		SetProcessInput(true);
	}

	public override void _Input(InputEvent @event)
	{
		if (_currentlyRebindingButton == null)
			return;
		
		// Only accept certain input types
		bool isValidInput = @event is InputEventKey or InputEventMouseButton or 
		                    InputEventJoypadButton or InputEventJoypadMotion;
		
		if (!isValidInput)
			return;
		
		// Don't accept modifier-only keys or mouse motion
		if (@event is InputEventKey key)
		{
			// Skip modifier keys by themselves
			if (key.Keycode == Key.Shift || key.Keycode == Key.Ctrl || 
			    key.Keycode == Key.Alt || key.Keycode == Key.Meta)
				return;
			
			// Only accept when pressed, not released
			if (!key.Pressed)
				return;
		}
		
		if (@event is InputEventMouseButton mb && !mb.Pressed)
			return;
		
		if (@event is InputEventJoypadButton jb && !jb.Pressed)
			return;
		
		// Remap the action
		_inputManager.RemapAction(_currentlyRebindingAction, @event, _currentlyRebindingIndex);
		
		// Update button text
		_currentlyRebindingButton.Text = InputManager.GetEventDisplayString(@event);
		
		// Clear rebinding state
		_currentlyRebindingButton = null;
		_currentlyRebindingAction = null;
		SetProcessInput(false);
		
		GetViewport().SetInputAsHandled();
		
		// Refresh the display
		PopulateBindings();
	}

	private void RestoreButtonText(Button button, string actionName, int index)
	{
		var events = _inputManager.GetActionEvents(actionName);
		if (index < events.Count)
		{
			button.Text = InputManager.GetEventDisplayString(events[index]);
		}
		else
		{
			button.Text = "Not Bound";
		}
	}

	private void OnResetActionPressed(string actionName)
	{
		_inputManager.ResetActionToDefault(actionName);
		PopulateBindings();
	}

	private void OnResetAllBindings()
	{
		_inputManager.ResetAllToDefaults();
		PopulateBindings();
	}

	private string FormatActionName(string actionName)
	{
		// Convert snake_case to Title Case
		string[] words = actionName.Split('_');
		for (int i = 0; i < words.Length; i++)
		{
			if (words[i].Length > 0)
			{
				words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
			}
		}
		return string.Join(" ", words);
	}

	private void ShowPanel(string panelName)
	{
		// Hide all panels
		_gameplayPanel.Visible = false;
		_videoPanel.Visible = false;
		_soundPanel.Visible = false;
		_keybindingsPanel.Visible = false;

		// Reset button states
		_gameplayButton.ButtonPressed = false;
		_videoButton.ButtonPressed = false;
		_soundButton.ButtonPressed = false;
		_keybindingsButton.ButtonPressed = false;

		// Show selected panel and highlight button
		switch (panelName)
		{
			case "gameplay":
				_gameplayPanel.Visible = true;
				_gameplayButton.ButtonPressed = true;
				_gameplayButton.GrabFocus();
				break;
			case "video":
				_videoPanel.Visible = true;
				_videoButton.ButtonPressed = true;
				_resolutionOption.GrabFocus();
				break;
			case "sound":
				_soundPanel.Visible = true;
				_soundButton.ButtonPressed = true;
				_backButton.GrabFocus();
				break;
			case "keybindings":
				_keybindingsPanel.Visible = true;
				_keybindingsButton.ButtonPressed = true;
				// Focus first binding button
				if (_bindingsContainer.GetChildCount() > 0)
				{
					var firstRow = _bindingsContainer.GetChild(0) as HBoxContainer;
					if (firstRow != null && firstRow.GetChildCount() > 1)
					{
						var firstButton = firstRow.GetChild(1) as Button;
						firstButton?.GrabFocus();
					}
				}
				break;
		}
	}

	private void OnBackPressed()
	{
		QueueFree();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Don't handle escape if currently rebinding
		if (_currentlyRebindingButton != null)
			return;
		
		// Close with Escape
		if (@event.IsActionPressed("ui_cancel"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}
}
