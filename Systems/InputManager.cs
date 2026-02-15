using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton InputManager for runtime key rebinding and input management.
/// Autoload this as "InputManager" in project settings.
/// </summary>
public partial class InputManager : Node
{
	private const string ConfigPath = "user://input_config.json";
	
	// Store default bindings for reset functionality
	private Dictionary<string, Godot.Collections.Array<InputEvent>> _defaultBindings = new();
	
	[Signal]
	public delegate void InputRemappedEventHandler(string actionName);

	public override void _Ready()
	{
		// Save default bindings from project.godot
		SaveDefaultBindings();
		
		// Load custom bindings if they exist
		LoadCustomBindings();
	}

	/// <summary>
	/// Save all default input bindings from project.godot for later reset
	/// </summary>
	private void SaveDefaultBindings()
	{
		var actions = GetAllActions();
		foreach (string action in actions)
		{
			var events = InputMap.ActionGetEvents(action);
			_defaultBindings[action] = new Godot.Collections.Array<InputEvent>(events);
		}
	}

	/// <summary>
	/// Get all input actions defined in the project
	/// </summary>
	public Godot.Collections.Array<StringName> GetAllActions()
	{
		return InputMap.GetActions();
	}

	/// <summary>
	/// Get all events for a specific action
	/// </summary>
	public Godot.Collections.Array<InputEvent> GetActionEvents(string actionName)
	{
		return InputMap.ActionGetEvents(actionName);
	}

	/// <summary>
	/// Remap an action to a new input event
	/// </summary>
	/// <param name="actionName">The action to remap</param>
	/// <param name="newEvent">The new input event</param>
	/// <param name="eventIndex">Which event to replace (0 for primary, 1+ for alternatives)</param>
	public bool RemapAction(string actionName, InputEvent newEvent, int eventIndex = 0)
	{
		if (!InputMap.HasAction(actionName))
		{
			GD.PrintErr($"Action '{actionName}' does not exist!");
			return false;
		}

		// Check for conflicts
		string conflictAction = FindConflictingAction(newEvent, actionName);
		if (conflictAction != null)
		{
			GD.Print($"Warning: Input conflicts with action '{conflictAction}'");
			// You can choose to block this or allow it - currently allowing
		}

		// Get current events
		var events = InputMap.ActionGetEvents(actionName);
		
		// Clear old binding at this index if it exists
		if (eventIndex < events.Count)
		{
			InputMap.ActionEraseEvent(actionName, events[eventIndex]);
		}
		
		// Add new binding
		InputMap.ActionAddEvent(actionName, newEvent);
		
		// Save to config
		SaveCustomBindings();
		
		EmitSignal(SignalName.InputRemapped, actionName);
		return true;
	}

	/// <summary>
	/// Add an alternative input to an action (doesn't replace existing)
	/// </summary>
	public void AddActionEvent(string actionName, InputEvent newEvent)
	{
		if (!InputMap.HasAction(actionName))
		{
			GD.PrintErr($"Action '{actionName}' does not exist!");
			return;
		}

		InputMap.ActionAddEvent(actionName, newEvent);
		SaveCustomBindings();
		EmitSignal(SignalName.InputRemapped, actionName);
	}

	/// <summary>
	/// Remove a specific event from an action
	/// </summary>
	public void RemoveActionEvent(string actionName, InputEvent eventToRemove)
	{
		if (!InputMap.HasAction(actionName))
		{
			GD.PrintErr($"Action '{actionName}' does not exist!");
			return;
		}

		InputMap.ActionEraseEvent(actionName, eventToRemove);
		SaveCustomBindings();
		EmitSignal(SignalName.InputRemapped, actionName);
	}

	/// <summary>
	/// Clear all events from an action
	/// </summary>
	public void ClearAction(string actionName)
	{
		if (!InputMap.HasAction(actionName))
		{
			GD.PrintErr($"Action '{actionName}' does not exist!");
			return;
		}

		var events = InputMap.ActionGetEvents(actionName);
		foreach (var evt in events)
		{
			InputMap.ActionEraseEvent(actionName, evt);
		}
		
		SaveCustomBindings();
		EmitSignal(SignalName.InputRemapped, actionName);
	}

	/// <summary>
	/// Reset a specific action to its default binding
	/// </summary>
	public void ResetActionToDefault(string actionName)
	{
		if (!_defaultBindings.ContainsKey(actionName))
		{
			GD.PrintErr($"No default binding found for '{actionName}'");
			return;
		}

		// Clear current bindings
		ClearAction(actionName);
		
		// Restore defaults
		foreach (var evt in _defaultBindings[actionName])
		{
			InputMap.ActionAddEvent(actionName, evt);
		}
		
		SaveCustomBindings();
		EmitSignal(SignalName.InputRemapped, actionName);
	}

	/// <summary>
	/// Reset all actions to their default bindings
	/// </summary>
	public void ResetAllToDefaults()
	{
		foreach (var kvp in _defaultBindings)
		{
			string actionName = kvp.Key;
			
			// Clear current
			var currentEvents = InputMap.ActionGetEvents(actionName);
			foreach (var evt in currentEvents)
			{
				InputMap.ActionEraseEvent(actionName, evt);
			}
			
			// Restore defaults
			foreach (var evt in kvp.Value)
			{
				InputMap.ActionAddEvent(actionName, evt);
			}
			
			EmitSignal(SignalName.InputRemapped, actionName);
		}
		
		SaveCustomBindings();
	}

	/// <summary>
	/// Find if a given input event conflicts with any action (except the one being remapped)
	/// </summary>
	private string FindConflictingAction(InputEvent newEvent, string excludeAction = "")
	{
		var actions = GetAllActions();
		
		foreach (string action in actions)
		{
			if (action == excludeAction)
				continue;
				
			var events = InputMap.ActionGetEvents(action);
			foreach (var evt in events)
			{
				if (EventsMatch(evt, newEvent))
				{
					return action;
				}
			}
		}
		
		return null;
	}

	/// <summary>
	/// Check if two input events are equivalent
	/// </summary>
	private bool EventsMatch(InputEvent evt1, InputEvent evt2)
	{
		// Keyboard
		if (evt1 is InputEventKey key1 && evt2 is InputEventKey key2)
		{
			return key1.PhysicalKeycode == key2.PhysicalKeycode || 
			       (key1.PhysicalKeycode == Key.None && key1.Keycode == key2.Keycode);
		}
		
		// Mouse button
		if (evt1 is InputEventMouseButton mb1 && evt2 is InputEventMouseButton mb2)
		{
			return mb1.ButtonIndex == mb2.ButtonIndex;
		}
		
		// Joypad button
		if (evt1 is InputEventJoypadButton jb1 && evt2 is InputEventJoypadButton jb2)
		{
			return jb1.ButtonIndex == jb2.ButtonIndex;
		}
		
		// Joypad motion
		if (evt1 is InputEventJoypadMotion jm1 && evt2 is InputEventJoypadMotion jm2)
		{
			return jm1.Axis == jm2.Axis && 
			       Mathf.Sign(jm1.AxisValue) == Mathf.Sign(jm2.AxisValue);
		}
		
		return false;
	}

	/// <summary>
	/// Save current bindings to user config file
	/// </summary>
	public void SaveCustomBindings()
	{
		var config = new Godot.Collections.Dictionary();
		var actions = GetAllActions();
		
		foreach (string action in actions)
		{
			var events = InputMap.ActionGetEvents(action);
			var serializedEvents = new Godot.Collections.Array();
			
			foreach (var evt in events)
			{
				serializedEvents.Add(SerializeInputEvent(evt));
			}
			
			config[action] = serializedEvents;
		}
		
		string jsonString = Json.Stringify(config, "\t");
		
		using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(jsonString);
			GD.Print("Input bindings saved to: ", ConfigPath);
		}
		else
		{
			GD.PrintErr("Failed to save input bindings!");
		}
	}

	/// <summary>
	/// Load custom bindings from user config file
	/// </summary>
	public void LoadCustomBindings()
	{
		if (!FileAccess.FileExists(ConfigPath))
		{
			GD.Print("No custom input config found, using defaults");
			return;
		}
		
		using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr("Failed to load input bindings!");
			return;
		}
		
		string jsonString = file.GetAsText();
		var json = new Json();
		var parseResult = json.Parse(jsonString);
		
		if (parseResult != Error.Ok)
		{
			GD.PrintErr("Failed to parse input config JSON!");
			return;
		}
		
		var config = json.Data.AsGodotDictionary();
		
		foreach (var key in config.Keys)
		{
			string actionName = key.ToString();
			
			if (!InputMap.HasAction(actionName))
				continue;
			
			// Clear current bindings
			var currentEvents = InputMap.ActionGetEvents(actionName);
			foreach (var evt in currentEvents)
			{
				InputMap.ActionEraseEvent(actionName, evt);
			}
			
			// Load saved bindings
			var serializedEvents = config[key].AsGodotArray();
			foreach (var serializedEvent in serializedEvents)
			{
				var evt = DeserializeInputEvent(serializedEvent.AsGodotDictionary());
				if (evt != null)
				{
					InputMap.ActionAddEvent(actionName, evt);
				}
			}
		}
		
		GD.Print("Custom input bindings loaded from: ", ConfigPath);
	}

	/// <summary>
	/// Convert InputEvent to dictionary for JSON serialization
	/// </summary>
	private Godot.Collections.Dictionary SerializeInputEvent(InputEvent evt)
	{
		var data = new Godot.Collections.Dictionary();
		
		if (evt is InputEventKey key)
		{
			data["type"] = "key";
			data["physical_keycode"] = (int)key.PhysicalKeycode;
			data["keycode"] = (int)key.Keycode;
		}
		else if (evt is InputEventMouseButton mb)
		{
			data["type"] = "mouse_button";
			data["button_index"] = (int)mb.ButtonIndex;
		}
		else if (evt is InputEventJoypadButton jb)
		{
			data["type"] = "joypad_button";
			data["button_index"] = (int)jb.ButtonIndex;
			data["device"] = jb.Device;
		}
		else if (evt is InputEventJoypadMotion jm)
		{
			data["type"] = "joypad_motion";
			data["axis"] = (int)jm.Axis;
			data["axis_value"] = jm.AxisValue;
			data["device"] = jm.Device;
		}
		
		return data;
	}

	/// <summary>
	/// Convert dictionary back to InputEvent
	/// </summary>
	private InputEvent DeserializeInputEvent(Godot.Collections.Dictionary data)
	{
		string type = data["type"].ToString();
		
		switch (type)
		{
			case "key":
				var key = new InputEventKey();
				key.PhysicalKeycode = (Key)(int)data["physical_keycode"];
				key.Keycode = (Key)(int)data["keycode"];
				return key;
				
			case "mouse_button":
				var mb = new InputEventMouseButton();
				mb.ButtonIndex = (MouseButton)(int)data["button_index"];
				return mb;
				
			case "joypad_button":
				var jb = new InputEventJoypadButton();
				jb.ButtonIndex = (JoyButton)(int)data["button_index"];
				jb.Device = (int)data["device"];
				return jb;
				
			case "joypad_motion":
				var jm = new InputEventJoypadMotion();
				jm.Axis = (JoyAxis)(int)data["axis"];
				jm.AxisValue = (float)data["axis_value"];
				jm.Device = (int)data["device"];
				return jm;
		}
		
		return null;
	}

	/// <summary>
	/// Get a human-readable string for an input event
	/// </summary>
	public static string GetEventDisplayString(InputEvent evt)
	{
		if (evt is InputEventKey key)
		{
			// Use physical keycode if available
			var keycode = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
			return OS.GetKeycodeString(keycode);
		}
		
		if (evt is InputEventMouseButton mb)
		{
			return mb.ButtonIndex switch
			{
				MouseButton.Left => "Left Mouse",
				MouseButton.Right => "Right Mouse",
				MouseButton.Middle => "Middle Mouse",
				MouseButton.WheelUp => "Wheel Up",
				MouseButton.WheelDown => "Wheel Down",
				MouseButton.WheelLeft => "Wheel Left",
				MouseButton.WheelRight => "Wheel Right",
				MouseButton.Xbutton1 => "Mouse X1",
				MouseButton.Xbutton2 => "Mouse X2",
				_ => $"Mouse {(int)mb.ButtonIndex}"
			};
		}
		
		if (evt is InputEventJoypadButton jb)
		{
			return $"Joy Button {(int)jb.ButtonIndex}";
		}
		
		if (evt is InputEventJoypadMotion jm)
		{
			string direction = jm.AxisValue > 0 ? "+" : "-";
			return $"Joy Axis {(int)jm.Axis}{direction}";
		}
		
		return evt.AsText();
	}
}
