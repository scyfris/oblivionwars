using Godot;
using System;

/// <summary>
/// Generates a test platforming level for testing mechanics like jumping, wall sliding, and gravity flipping.
/// </summary>
public partial class TestLevelGenerator : Node
{
	/// <summary>
	/// The TileMapLayer to generate the level into (must be assigned in the editor)
	/// </summary>
	[Export] private TileMapLayer _tileMapLayer;

	/// <summary>
	/// Width of the generated level in tiles
	/// </summary>
	[Export] private int _levelWidth = 10;

	/// <summary>
	/// Height of the generated level in tiles
	/// </summary>
	[Export] private int _levelHeight = 10;

	/// <summary>
	/// Atlas coordinates for solid ground/wall tiles (e.g., boundaries and solid platforms)
	/// </summary>
	[Export] private Vector2I _groundTileAtlasCoord = new Vector2I(0, 0);

	/// <summary>
	/// Atlas coordinates for platform tiles (e.g., floating platforms and obstacles)
	/// </summary>
	[Export] private Vector2I _platformTileAtlasCoord = new Vector2I(1, 0);

	public override void _Ready()
	{
		if (_tileMapLayer == null)
		{
			GD.PrintErr("TestLevelGenerator: TileMapLayer not assigned!");
			return;
		}

		GenerateLevel();
		GD.Print("TestLevelGenerator: Level generated!");
	}

	private void GenerateLevel()
	{
		// Clear existing tiles
		_tileMapLayer.Clear();

		// Create floor (bottom of level)
		for (int x = 0; x < _levelWidth; x++)
		{
			_tileMapLayer.SetCell(new Vector2I(x, _levelHeight - 1), 0, _groundTileAtlasCoord);
			_tileMapLayer.SetCell(new Vector2I(x, _levelHeight - 2), 0, _groundTileAtlasCoord);
		}

		// Create left wall
		for (int y = 0; y < _levelHeight; y++)
		{
			_tileMapLayer.SetCell(new Vector2I(0, y), 0, _groundTileAtlasCoord);
		}

		// Create right wall
		for (int y = 0; y < _levelHeight; y++)
		{
			_tileMapLayer.SetCell(new Vector2I(_levelWidth - 1, y), 0, _groundTileAtlasCoord);
		}

		// Create ceiling
		for (int x = 0; x < _levelWidth; x++)
		{
			_tileMapLayer.SetCell(new Vector2I(x, 0), 0, _groundTileAtlasCoord);
		}

		// Platform 1: Low platform (easy jump)
		CreatePlatform(10, 85, 8);

		// Platform 2: Medium height
		CreatePlatform(20, 75, 6);

		// Platform 3: Higher platform
		CreatePlatform(30, 65, 8);

		// Platform 4: Wall jump test - single block wall
		CreateVerticalWall(40, 55, 15);

		// Platform 5: After wall jump
		CreatePlatform(42, 55, 6);

		// Platform 6: Staircase going up
		CreateStaircase(50, 80, 10, true);

		// Platform 7: Long platform at mid level
		CreatePlatform(15, 55, 20);

		// Platform 8: Floating platforms for jumping practice
		CreatePlatform(38, 45, 4);
		CreatePlatform(44, 40, 4);
		CreatePlatform(50, 35, 4);

		// Platform 9: Vertical shaft for gravity flip testing
		CreateVerticalShaft(60, 20, 60, 15);

		// Platform 10: Horizontal platforms in shaft
		CreatePlatform(62, 30, 3);
		CreatePlatform(68, 40, 3);
		CreatePlatform(62, 50, 3);
		CreatePlatform(68, 60, 3);

		// Platform 11: Wall jump challenge - narrow vertical space
		CreateVerticalWall(75, 70, 20);
		CreateVerticalWall(80, 70, 20);
		// Platforms between walls
		CreatePlatform(76, 65, 3);
		CreatePlatform(76, 55, 3);

		// Platform 12: Return path - descending platforms
		CreatePlatform(85, 60, 8);
		CreatePlatform(90, 70, 6);
		CreatePlatform(85, 80, 8);

		// Obstacle course area
		CreatePlatform(5, 40, 3);
		CreatePlatform(10, 35, 3);
		CreatePlatform(5, 30, 3);
		CreatePlatform(10, 25, 3);

		// Create some enclosed spaces for gravity flip fun
		CreateBox(25, 20, 8, 8);
		CreateBox(35, 15, 6, 10);

		GD.Print($"TestLevelGenerator: Created level {_levelWidth}x{_levelHeight}");
	}

	/// <summary>
	/// Creates a horizontal platform
	/// </summary>
	private void CreatePlatform(int startX, int y, int length)
	{
		for (int x = startX; x < startX + length && x < _levelWidth; x++)
		{
			_tileMapLayer.SetCell(new Vector2I(x, y), 0, _platformTileAtlasCoord);
		}
	}

	/// <summary>
	/// Creates a vertical wall
	/// </summary>
	private void CreateVerticalWall(int x, int startY, int height)
	{
		for (int y = startY; y < startY + height && y < _levelHeight; y++)
		{
			_tileMapLayer.SetCell(new Vector2I(x, y), 0, _groundTileAtlasCoord);
		}
	}

	/// <summary>
	/// Creates a staircase
	/// </summary>
	private void CreateStaircase(int startX, int startY, int steps, bool ascending)
	{
		for (int i = 0; i < steps; i++)
		{
			int x = startX + i;
			int y = ascending ? startY - i : startY + i;

			if (x >= _levelWidth || y < 0 || y >= _levelHeight) break;

			_tileMapLayer.SetCell(new Vector2I(x, y), 0, _platformTileAtlasCoord);
			// Add a second tile below for thickness
			if (y + 1 < _levelHeight)
			{
				_tileMapLayer.SetCell(new Vector2I(x, y + 1), 0, _groundTileAtlasCoord);
			}
		}
	}

	/// <summary>
	/// Creates a vertical shaft (hollow vertical space with walls)
	/// </summary>
	private void CreateVerticalShaft(int startX, int startY, int height, int width)
	{
		// Left wall
		CreateVerticalWall(startX, startY, height);

		// Right wall
		CreateVerticalWall(startX + width - 1, startY, height);

		// Top
		CreatePlatform(startX, startY, width);

		// Bottom
		CreatePlatform(startX, startY + height - 1, width);
	}

	/// <summary>
	/// Creates a box (enclosed space)
	/// </summary>
	private void CreateBox(int startX, int startY, int width, int height)
	{
		// Top
		CreatePlatform(startX, startY, width);

		// Bottom
		CreatePlatform(startX, startY + height - 1, width);

		// Left
		CreateVerticalWall(startX, startY, height);

		// Right
		CreateVerticalWall(startX + width - 1, startY, height);
	}
}
