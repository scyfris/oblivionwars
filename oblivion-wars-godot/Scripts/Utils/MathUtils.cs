using Godot;
using System;

/// <summary>
/// Global utility functions for mathematical operations
/// </summary>
public static class MathUtils
{
	/// <summary>
	/// Snaps an angle in degrees to the nearest 90-degree increment.
	/// Examples: 0->0, 45->0, 46->90, 90->90, 177->180, -173->-180
	/// </summary>
	public static float SnapToNearest90Degrees(float degrees)
	{
		return Mathf.Round(degrees / 90.0f) * 90.0f;
	}

	/// <summary>
	/// Snaps an angle in radians to the nearest 90-degree (π/2) increment.
	/// </summary>
	public static float SnapToNearest90Radians(float radians)
	{
		float halfPi = Mathf.Pi / 2.0f;
		return Mathf.Round(radians / halfPi) * halfPi;
	}

	/// <summary>
	/// Normalizes an angle in degrees to the range [0, 360)
	/// </summary>
	public static float NormalizeAngleDegrees(float degrees)
	{
		degrees = degrees % 360.0f;
		if (degrees < 0)
			degrees += 360.0f;
		return degrees;
	}

	/// <summary>
	/// Normalizes an angle in radians to the range [0, 2π)
	/// </summary>
	public static float NormalizeAngleRadians(float radians)
	{
		radians = radians % (2.0f * Mathf.Pi);
		if (radians < 0)
			radians += 2.0f * Mathf.Pi;
		return radians;
	}
}
