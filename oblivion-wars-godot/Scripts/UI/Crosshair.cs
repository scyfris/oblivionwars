using Godot;
using System;

public partial class Crosshair : Sprite2D
{
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden; // Hide default cursor
	}

	public override void _Process(double delta)
	{
		GlobalPosition = GetGlobalMousePosition(); // Follow mouse
	}
}
