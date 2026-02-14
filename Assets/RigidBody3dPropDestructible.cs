using Godot;
using System;

// Remember to have Check Monitor enabled for the Node and Max Contacts Reported appropriate.

public partial class RigidBody3dPropDestructible : RigidBody3D
{
	[Export] public bool DebugMode = false;
	[Export] public bool DebrisEnabled = true;
	[Export] public int DebrisAmount = 4;


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
		if (DebugMode) GD.Print($"Health: {current}/{max}");
	}

	private void OnHealthEmpty()
	{
		// Play effect/animation
		// if there is an effect play this:
		// GetTree().CreateTimer(1.0).Timeout += QueueFree; //(one second delay to avoid effects vanishing)
		// OR spawn smaller wreckage pieces and hide/remove the first original mesh
		// For now just this until effects/sound/etc. is added:
		if (DebrisEnabled)
		{
			SpawnDebris();
		}
		QueueFree();
	}

	private void OnBodyEntered(Node body)
	{
		if (DebugMode) GD.Print($"Body entered: {body.Name} (Type: {body.GetType().Name})");
		_health.TakeDamage(20);
	}

	private void SpawnDebris()
	{
		// Debris to be spawned
		var debrisScene = GD.Load<PackedScene>("res://Assets/RigidBody3DProp.tscn");

		for (int i = 0; i < DebrisAmount; i++)
		{
			var debris = debrisScene.Instantiate<RigidBody3D>();

			// Add to scene (same parent as this object)
			GetParent().AddChild(debris);

			// Position at this object's location
			debris.GlobalPosition = GlobalPosition;

			// Random impulse/spin for visual effect
			Vector3 randomDirection = new Vector3(
				(float)GD.RandRange(-1, 1), //Horizontal X
				(float)GD.RandRange(15, 25), //Vertical Y
				(float)GD.RandRange(-1, 1) //Horizontal Z
			);

			debris.ApplyCentralImpulse(randomDirection * 5.0f);
			debris.ApplyTorqueImpulse(new Vector3(
				(float)GD.RandRange(-2, 2),
				(float)GD.RandRange(-2, 2),
				(float)GD.RandRange(-2, 2)
			));
		}
	}

}
