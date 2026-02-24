using Godot;

/// <summary>
/// Centralized debug system for controlling debug output across the entire project.
/// Provides per-system debug flags for granular control without scattered conditionals.
/// </summary>
public static class DebugManager
{
	/// <summary>
	/// Master toggle for all debug output.
	/// </summary>
	public static bool Enabled { get; set; } = true;

	/// <summary>
	/// Debug output for ledge grab system.
	/// </summary>
	public static bool LedgeGrab { get; set; } = true;

	/// <summary>
	/// Debug output for player movement/state.
	/// </summary>
	public static bool PlayerMovement { get; set; } = false;

	/// <summary>
	/// Debug output for player state transitions.
	/// </summary>
	public static bool PlayerStateTransitions { get; set; } = false;

	/// <summary>
	/// Debug output for rig/animation state.
	/// </summary>
	public static bool RigAnimations { get; set; } = true;

	/// <summary>
	/// Check if a specific debug category is enabled.
	/// </summary>
	public static bool IsEnabled(bool category)
	{
		return Enabled && category;
	}

	/// <summary>
	/// Utility method to print debug messages with category prefix.
	/// </summary>
	public static void Print(string category, params Variant[] args)
	{
		if (Enabled)
		{
			GD.Print($"[{category}] ", args);
		}
	}
}
