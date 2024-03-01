using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// a port of this to C#:  https://github.com/russs123/tetris_tut/blob/main/TileMap.gd

public partial class MainScript : TileMap
{
	public static readonly Vector2I[] i_0 = [new(0, 1), new(1, 1), new(2, 1), new(3, 1)];
	public static readonly Vector2I[] i_90 = [new(2, 0), new(2, 1), new(2, 2), new(2, 3)];
	public static readonly Vector2I[] i_180 = [new(0, 2), new(1, 2), new(2, 2), new(3, 2)];
	public static readonly Vector2I[] i_270 = [new(1, 0), new(1, 1), new(1, 2), new(1, 3)];

	public static readonly Vector2I[][] i = [i_0, i_90, i_180, i_270];

	public static readonly Vector2I[] t_0 = [new(1, 0), new(0, 1), new(1, 1), new(2, 1)];
	public static readonly Vector2I[] t_90 = [new(1, 0), new(1, 1), new(2, 1), new(1, 2)];
	public static readonly Vector2I[] t_180 = [new(0, 1), new(1, 1), new(2, 1), new(1, 2)];
	public static readonly Vector2I[] t_270 = [new(1, 0), new(0, 1), new(1, 1), new(1, 2)];

	public static readonly Vector2I[][] t = [t_0, t_90, t_180, t_270];

	public static readonly Vector2I[][][] AllShapes = [i, t];

	private Stack<Vector2I[][]> next_shapes;

	public static Stack<Vector2I[][]> GetShuffledShapes()
	{
		var result = new Stack<Vector2I[][]>();
		var indexes = Enumerable.Range(0, AllShapes.Length).ToArray();
		Random.Shared.Shuffle(indexes);

		foreach (var i in indexes)
		{
			result.Push(AllShapes[i]);
		}
		return result;
	}

	// Grid Variables
	public const int COLS = 10;
	public const int ROWS = 20;

	// Movement Variables.
	public static readonly Vector2I START_POSITION = new(5, 1);
	public Vector2I currentPosition;

	public static readonly Vector2I[] directions = [Vector2I.Left, Vector2I.Right, Vector2I.Down];
	public const double INITIAL_SPEED = 1.0;
	public double Speed;
	public double[] steps = [0, 0, 0];  // LEFT, RIGHT, DOWN

	private Vector2I[][] PieceType;
	private Vector2I[][] NextPieceType;
	/// <summary>
	/// The current rotation index: 0 => 0, 90 => 1, 180 => 2, 270 => 3
	/// </summary>
	private int rotationIndex = 0;
	private Vector2I[] activePiece;

	private int tileId = 0;

	/// <summary>
	/// Indexes into the Tetronimoes atlas - it's a Vector2I because the atlas is 2d (even though it's all on one row)
	/// This is basically the way to get the color of the blocks in the shape.
	/// </summary>
	public Vector2I PieceAtlas;
	public Vector2I NextPieceAtlas;

	public const int BOARD_LAYER = 0;
	public const int ACTIVE_LAYER = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		NewGame();
	}

	public Node Hud => FindChild("HUD", recursive: false);

	private void NewGame()
	{
		(Hud.GetNode("GameOverLabel") as Label).Hide();
		
		// reset variables
		Speed = INITIAL_SPEED;

		PieceType = PickPiece();
		// find the corresponding color in the piece atlas - since it's flat, the y is always 0.
		PieceAtlas = new Vector2I(Array.IndexOf(AllShapes, PieceType), 0);

		NextPieceType = PickPiece();
		NextPieceAtlas = new Vector2I(Array.IndexOf(AllShapes, NextPieceType), 0);

		CreatePiece();
	}

	private Vector2I[][] PickPiece()
	{
		if (next_shapes is not { Count: > 0 } )
		{
			next_shapes = GetShuffledShapes();
		}
		return next_shapes.Pop();
	}

	/// <summary>
	/// creates a piece at the start position and draws it.
	/// </summary>
	private void CreatePiece()
	{
		// reset the variables.
		steps = [0, 0, 0];
		currentPosition = START_POSITION;
		activePiece = PieceType[rotationIndex];
		DrawPiece(activePiece, currentPosition, PieceAtlas);
		DrawPiece(NextPieceType[0], new Vector2I(15, 6), NextPieceAtlas);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("ui_down"))
		{
			steps[2] += delta * 20;
		}

		if (Input.IsActionJustPressed("ui_up"))
		{
			TryRotatePiece();
		}

		if (Input.IsActionPressed("ui_left"))
		{
			steps[0] += delta * 14;
		}
		else if (Input.IsActionPressed("ui_right"))
		{
			steps[1] += delta * 14;
		}

		steps[2] += delta;
		
		for (var i = 0; i < directions.Length; i++)
		{
			if (steps[i] > Speed)
			{
				TryMovePiece(directions[i]);
				steps[i] = 0;
			}
		}
	}

	private void TryRotatePiece()
	{
		if (CanRotate())
		{
			EraseActivePiece();
			rotationIndex = (rotationIndex + 1) % 4; // caps us with 0-3
			activePiece = PieceType[rotationIndex];
			DrawPiece(activePiece, currentPosition, PieceAtlas);
		}
	}

	private void TryMovePiece(Vector2I direction)
	{
		if (CanMove(direction))
		{
			EraseActivePiece();
			currentPosition += direction;
			DrawPiece(activePiece, currentPosition, PieceAtlas);
		}
		else
		{
			if (direction == Vector2I.Down)
			{
				LandPiece();
				CheckRows();
				PieceType = NextPieceType;
				PieceAtlas = NextPieceAtlas;
				NextPieceType = PickPiece();
				NextPieceAtlas = new Vector2I(Array.IndexOf(AllShapes, NextPieceType), 0);
				ClearNextPanel();
				CreatePiece();
			}
		}
	}

	private void ClearNextPanel()
	{
		// this is a brute force way to clear the next panel
		for (var x = 14; x < 20; x++)
		{
			for (var y = 5; y < 10; y++)
			{
				EraseCell(ACTIVE_LAYER, new Vector2I(x, y));
			}
		}
	}

	/// <summary>
	/// Remove each segment from the active layer and move to the board layer.
	/// </summary>
	private void LandPiece()
	{
		for (var i = 0; i < activePiece.Length; i++)
		{
			EraseCell(ACTIVE_LAYER, currentPosition + activePiece[i]);
			SetCell(BOARD_LAYER, currentPosition + activePiece[i], tileId, PieceAtlas);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="piece"></param>
	/// <param name="position"></param>
	/// <param name="atlas">Refers to the position in the atlas (always will be in the Active layer)</param>
	public void DrawPiece(Vector2I[] piece, Vector2I position, Vector2I atlas)
	{
		foreach (var part in piece)
		{
			SetCell(ACTIVE_LAYER, position + part, tileId, atlas);
		}
	}

	public void EraseActivePiece()
	{
		foreach (var part in activePiece)
		{
			EraseCell(ACTIVE_LAYER, currentPosition + part);
		}
	}
	
	/// <summary>
	/// Returns true if the active piece is free to move in this direction.
	/// </summary>
	public bool CanMove(Vector2I direction)
	{
		for (int i = 0; i < activePiece.Length; i++)
		{
			if (!IsPositionFree(activePiece[i] + direction + currentPosition))
			{
				return false;
			}
		}
		return true;
	}

	private bool CanRotate()
	{
		var nextRotationIndex = (rotationIndex + 1) % 4;
		var rotatedPiece = PieceType[nextRotationIndex];

		for (var i = 0; i < rotatedPiece.Length; i++)
		{
			if (!IsPositionFree(currentPosition + rotatedPiece[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsPositionFree(Vector2I position) {
		// NOTE: -1 is the empty tile.
		return GetCellSourceId(BOARD_LAYER, position) == -1;
	}

	public void CheckRows()
	{
		var row = ROWS - 1;
		while (row > 0)
		{
			var count = 0;
			for (var x = 0; x < COLS; x++)
			{
				if (IsPositionFree(new(x + 1, row)))
				{
					break;
				}
				else
				{
					count++;
				}
			}
			if (count == COLS)
			{
				Debug.WriteLine($"Completed line at row {row}!");
				ShiftRows(row);
			}
			else
			{
				row -= 1;
			}
		}
	}

	public void ShiftRows(int row)
	{
		for (var y = row; y > 1; y--)
		{
			for (var x = 0; x < COLS; x++)
			{
				var atlas = GetCellAtlasCoords(BOARD_LAYER, new Vector2I(x + 1, y - 1));

				if (atlas == new Vector2I(-1, -1))
				{
					EraseCell(BOARD_LAYER, new Vector2I(x + 1, y));
				}
				else
				{
					SetCell(BOARD_LAYER, new Vector2I(x + 1, y), tileId, atlas);
				}
			}
		}
	}
}
