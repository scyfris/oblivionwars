using Godot;

public partial class Crosshair : Node2D
{
	[Export] private Color _circleColor = new Color(1f, 1f, 1f, 0.6f);
	[Export] private float _lineWidth = 1.5f;
	/// <summary>Minimum circle radius in pixels when spread is zero (dot crosshair).</summary>
	[Export] private float _minRadius = 3f;
	/// <summary>Small center dot radius.</summary>
	[Export] private float _dotRadius = 2f;
	/// <summary>Pixels of radius per degree of spread.</summary>
	[Export] private float _pixelsPerDeg = 4f;

	[ExportGroup("Recoil Jitter")]
	/// <summary>Maximum jitter offset in pixels when recoil is at max.</summary>
	[Export] private float _maxJitterPx = 3f;

	[ExportGroup("Spread Cone")]
	[Export] private Color _coneColor = new Color(1f, 1f, 1f, 0.08f);
	[Export] private Color _coneEdgeColor = new Color(1f, 1f, 1f, 0.15f);
	[Export] private float _coneEdgeWidth = 1f;

	private PlayerController _playerController;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
	}

	public override void _Process(double delta)
	{
		GlobalPosition = GetGlobalMousePosition();

		if (_playerController == null)
		{
			var playerBody = GetTree().GetFirstNodeInGroup(GroupConstants.Entities.Player) as PlayerCharacterBody2D;
			_playerController = playerBody?.Controller;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		float spreadDeg = _playerController?.ActiveWeapon?.TotalSpreadRadiusDeg ?? 0f;
		float radius = Mathf.Max(_minRadius, spreadDeg * _pixelsPerDeg);

		// Jitter based on how close recoil is to max
		Vector2 jitter = Vector2.Zero;
		var weapon = _playerController?.ActiveWeapon;
		if (weapon != null && _maxJitterPx > 0f)
		{
			float maxRecoil = weapon.MaxRecoilDeg;
			if (maxRecoil > 0f)
			{
				float recoilRatio = weapon.CurrentRecoilDeg / maxRecoil;
				float jitterAmount = recoilRatio * _maxJitterPx;
				jitter = new Vector2(
					(float)GD.RandRange(-jitterAmount, jitterAmount),
					(float)GD.RandRange(-jitterAmount, jitterAmount)
				);
			}
		}

		// Center dot
		DrawCircle(jitter, _dotRadius, _circleColor);

		// Spread ring
		DrawArc(jitter, radius, 0f, Mathf.Tau, 64, _circleColor, _lineWidth);

		// Spread cone (debug gizmo)
		var global = GlobalStateManager.Instance?.Global;
		if (global?.ShowSpreadCone == true && spreadDeg > 0f && _playerController?.ActiveWeapon != null)
		{
			DrawSpreadCone(spreadDeg);
		}
	}

	private void DrawSpreadCone(float spreadDeg)
	{
		// Weapon is in world space; crosshair is under a CanvasLayer.
		// Convert weapon world position to screen, then to crosshair local space.
		// Weapon's on-screen position via its canvas transform
		Vector2 weaponScreen = _playerController.ActiveWeapon.GetGlobalTransformWithCanvas().Origin;
		// Crosshair's on-screen position via our canvas transform
		Vector2 crosshairScreen = GetCanvasTransform() * GlobalPosition;
		Vector2 localOrigin = weaponScreen - crosshairScreen;

		Vector2 toMouse = -localOrigin; // direction from weapon to crosshair in local space
		float distance = toMouse.Length();
		if (distance < 1f) return;

		float spreadRad = Mathf.DegToRad(spreadDeg);
		float baseAngle = toMouse.Angle();

		// Two edge points at the crosshair distance
		Vector2 edgeLeft = localOrigin + Vector2.FromAngle(baseAngle + spreadRad) * distance;
		Vector2 edgeRight = localOrigin + Vector2.FromAngle(baseAngle - spreadRad) * distance;

		// Semi-transparent filled triangle
		DrawPolygon(
			new Vector2[] { localOrigin, edgeLeft, edgeRight },
			new Color[] { _coneColor, _coneColor, _coneColor }
		);

		// Edge lines
		DrawLine(localOrigin, edgeLeft, _coneEdgeColor, _coneEdgeWidth);
		DrawLine(localOrigin, edgeRight, _coneEdgeColor, _coneEdgeWidth);
	}
}
