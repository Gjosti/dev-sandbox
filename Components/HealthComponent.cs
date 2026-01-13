using Godot;
using System;

// This node is meant to be a subnode and attached to mobs/objects/walls that can take damage.
// It holds max health points, current health.
// The node this is attached to should handle how an object behaves after health is reduced to 0 and other effects.

public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void HealthEmptyEventHandler();

	[Export] public bool Enabled = true;
	[Export] public bool DebugMode = false;
	[ExportGroup("Health Settings")]
	[Export] public int MaxHealth { get; set; } = 100;
	[Export] public int CurrentHealth { get; set; } = 100;
	[Export] public bool HealthRegen = false;
	[Export] public double HealthRegenRate = 1; //How often health is regained in seconds
	[Export] public int HealthRegenPotency = 1; // Amount of health regained per HealthRegenRate.

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void TakeDamage(int amount)
	{
		if (!Enabled) return;

		CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		if (DebugMode) GD.Print("The box took damage!" + CurrentHealth + "out of " + MaxHealth);

		if (CurrentHealth <= 0)
			EmitSignal(SignalName.HealthEmpty);

	}

	public void Heal(int amount)
	{
		if (!Enabled) return;

		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		if (CurrentHealth <= 0)
			EmitSignal(SignalName.HealthEmpty);

	}
}
