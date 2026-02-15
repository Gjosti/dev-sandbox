# InputManager System - Usage Guide

## Overview
The InputManager system provides runtime key rebinding support with save/load functionality, conflict detection, and the ability to reset to defaults.

## Architecture

### Components
1. **InputManager.cs** - Singleton autoload that manages input remapping
   - Location: `Systems/InputManager.cs`
   - Autoload name: `InputManager`
   - Handles saving/loading custom bindings to `user://input_config.json`

2. **InputSettingsUI.cs** - User interface for rebinding keys
   - Location: `UI/InputSettingsUI.cs`
   - Scene: `UI/input_settings_ui.tscn`
   - Accessible via "Input Settings" button in top-left of game UI
   - Also toggleable with ESC key

3. **UserInterface.cs** - Main UI that integrates the settings menu
   - Loads custom bindings on startup
   - Opens settings menu when button is clicked or ESC is pressed

## Features

### Automatic Binding Persistence
- Custom bindings are saved to `user://input_config.json`
- Automatically loaded on game startup
- Survives game restarts

### Conflict Detection
- Prevents binding the same key to multiple actions
- Shows warning when attempting duplicate bindings
- Automatically clears the conflicting action

### Reset to Defaults
- Reset individual actions to their original bindings
- Reset all actions at once with "Reset All to Defaults" button
- Original bindings from `project.godot` are always preserved

### Multi-Input Support
- Primary and secondary bindings for each action
- Supports keyboard, mouse, and gamepad inputs
- Human-readable input names (e.g., "Left Mouse", "Space", "W")

## How to Use (Player)

### Rebinding Keys
1. Click "Input Settings" button in top-left corner (or press ESC)
2. Find the action you want to rebind
3. Click the button showing the current binding
4. Press the new key/button you want to use
5. The binding updates immediately
6. Close the menu (bindings are auto-saved)

### Resetting Bindings
- **Single Action**: Click the "Reset" button next to the action
- **All Actions**: Click "Reset All to Defaults" at the bottom of the menu

### Supported Input Types
- Keyboard keys
- Mouse buttons (left, right, middle, side buttons)
- Gamepad buttons
- Gamepad analog axes

## How to Use (Developer)

### Accessing InputManager in Code
```csharp
// Get the singleton
var inputManager = GetNode<InputManager>("/root/InputManager");

// Or use it directly (it's an autoload)
InputManager.GetEventDisplayString(inputEvent);
```

### Programmatically Remapping Actions
```csharp
var inputManager = GetNode<InputManager>("/root/InputManager");

// Create a new input event
var newEvent = new InputEventKey();
newEvent.Keycode = Key.E;

// Remap the action (index 0 = primary binding)
inputManager.RemapAction("jump", newEvent, 0);
```

### Subscribing to Input Changes
```csharp
public override void _Ready()
{
    var inputManager = GetNode<InputManager>("/root/InputManager");
    inputManager.InputRemapped += OnInputRemapped;
}

private void OnInputRemapped(string actionName)
{
    GD.Print($"Action '{actionName}' was remapped!");
    // Update UI, refresh controls, etc.
}
```

### Getting Current Bindings
```csharp
var inputManager = GetNode<InputManager>("/root/InputManager");

// Get all events for an action
var events = inputManager.GetActionEvents("jump");

// Get display string for an event
string displayName = InputManager.GetEventDisplayString(events[0]);
// Returns "Space", "W", "Left Mouse", etc.
```

### Resetting Programmatically
```csharp
var inputManager = GetNode<InputManager>("/root/InputManager");

// Reset one action
inputManager.ResetActionToDefault("jump");

// Reset everything
inputManager.ResetAllToDefaults();
```

## Implementation Details

### Initialization Flow
1. Game starts → InputManager autoload created
2. UserInterface._Ready() → Calls `InputManager.LoadCustomBindings()`
3. If `user://input_config.json` exists → Loads custom bindings
4. If not → Uses default bindings from `project.godot`

### Saving Flow
1. User changes a binding in InputSettingsUI
2. InputSettingsUI calls `InputManager.RemapAction()`
3. InputManager updates the binding in InputMap
4. InputManager automatically saves to `user://input_config.json`
5. InputManager emits `InputRemapped` signal

### Data Format
Bindings are stored in JSON format:
```json
{
  "jump": [
    {
      "type": "InputEventKey",
      "keycode": 32,
      "physical_keycode": 32
    }
  ],
  "dash": [
    {
      "type": "InputEventKey",
      "keycode": 4194325,
      "physical_keycode": 4194325
    }
  ]
}
```

## Extending the System

### Adding New Actions
1. Add the action to `project.godot` under `[input]`
2. Assign default key(s) in the editor
3. InputManager will automatically detect and support it
4. It will appear in the InputSettingsUI automatically

### Customizing the UI
- Edit `UI/input_settings_ui.tscn` for visual changes
- Modify `InputSettingsUI.cs` to change behavior
- Override `FormatActionName()` for custom display names
- Filter actions in `PopulateBindings()` to hide certain inputs

### UI Filtering
By default, the settings UI hides `ui_*` actions (built-in Godot actions).
To change this, modify the filter in `InputSettingsUI.PopulateBindings()`:
```csharp
// Current filter (hides ui_ actions)
if (!action.ToString().StartsWith("ui_"))
{
    gameActions.Add(action);
}

// Show everything
gameActions.Add(action);

// Custom filter
if (action.ToString().StartsWith("player_"))
{
    gameActions.Add(action);
}
```

## File Locations

### Code Files
- `Systems/InputManager.cs` - Core input management singleton
- `UI/InputSettingsUI.cs` - Settings UI script
- `UI/UserInterface.cs` - Main UI integration

### Scene Files
- `UI/input_settings_ui.tscn` - Settings menu scene
- `UI/user_interface.tscn` - Main game UI

### Data Files
- `user://input_config.json` - Saved custom bindings (created at runtime)
- `project.godot` - Default input bindings

### Resource Files
- `Systems/InputManager.cs.uid` - Godot resource UID
- `UI/InputSettingsUI.cs.uid` - Godot resource UID

## Current Input Actions
- `jump` - Space (default)
- `dash` - Left Shift (default)
- `crouch` - Left Ctrl (default)
- `attack` - Left Mouse Button (default)
- `move_forward` - W
- `move_backward` - S
- `move_left` - A
- `move_right` - D
- `scroll_forward` - Mouse Wheel Up
- `scroll_backward` - Mouse Wheel Down

All actions support rebinding and will remember custom bindings across sessions.

## Troubleshooting

### Bindings Not Saving
- Check that `user://` directory is writable
- Look for errors in the console
- Verify InputManager is registered as autoload

### Bindings Not Loading on Startup
- Ensure `UserInterface._Ready()` calls `LoadCustomBindings()`
- Check that InputManager autoload comes before other scripts
- Verify `user://input_config.json` exists and is valid JSON

### Conflicts Not Detected
- Make sure you're using `RemapAction()` and not modifying InputMap directly
- Check that `EventsMatch()` covers your input type
- Verify no code is bypassing the InputManager

### UI Not Appearing
- Ensure `InputSettingsScene` is assigned in UserInterface inspector
- Check that `input_settings_ui.tscn` is properly set up
- Verify UI layer/z-index allows visibility

## Notes
- The system preserves original bindings from `project.godot` for reset functionality
- Multiple inputs can be bound to the same action (primary + secondary)
- Dead zones from `project.godot` are preserved
- Gamepad support includes buttons and analog stick axes
- All changes take effect immediately without restart
