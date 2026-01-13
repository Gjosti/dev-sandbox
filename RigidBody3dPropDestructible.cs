using Godot;
using System;

// Remember to have Check Monitor enabled for the Node and Max Contacts Reported appropriate.

public partial class RigidBody3dPropDestructible : RigidBody3D
{
	[Export] public bool DebugMode = false;


	private HealthComponent _health;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_health = GetNode<HealthComponent>("HealthComponent");
		_health.HealthChanged += OnHealthChanged;
		_health.HealthEmpty += OnHealthEmpty;

		BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnHealthChanged(int current, int max)
	{
		GD.Print($"Health: {current}/{max}");
	}

	private void OnHealthEmpty()
	{
		// Play effect/animation
		// if there is an effect play this:
		// GetTree().CreateTimer(1.0).Timeout += QueueFree; //(one second delay to avoid effects vanishing)
		// For now just this until effects/sound/etc. is added:
		QueueFree();
	}

	private void OnBodyEntered(Node body)
	{
		if (DebugMode) GD.Print($"Body entered: {body.Name} (Type: {body.GetType().Name})");
		_health.TakeDamage(10);
	}

}
