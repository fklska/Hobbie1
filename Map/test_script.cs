using Godot;
using System;

public partial class test_script : Node2D
{
	public NavigationRegion2D nav_region;
	[Export]
	public FastNoiseLite noise;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		nav_region = GetNode<NavigationRegion2D>("NavigationRegion2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
