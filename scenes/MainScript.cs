using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

	public static Stack<Vector2I[][]> shapes;

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

	//var shapes := [i, t, o, z, s, l, j]
	//var shapes_full := shapes.duplicate()

	// Grid Variables:
	const int COLS = 10;
	const int ROWS = 20;

	public Vector2I[][] PieceType { get; set; }
	public Vector2I[][] NextPieceType { get; set; }
	public int RotationIndex { get; set; } = 0;
	public Vector2I[] ActivePiece { get; set; }

	public int TileId { get; set; } = 0;
	/// <summary>
	/// Indexes into the Tetronimoes atlas - it's a Vector2I because the atlas is 2d (even though it's all on one row)
	/// </summary>
	public Vector2I PieceAtlas { get; set; }
	public Vector2I NextPieceAtlas { get; set; }

	public const int BOARD_LAYER = 0;
	public const int ACTIVE_LAYER = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		NewGame();
	}

	private void NewGame()
	{
		PieceType = PickPiece();
	}

	private Vector2I[][] PickPiece()
	{
		if (shapes is not { Count: > 0 } )
		{
			shapes = GetShuffledShapes();
		}
		return shapes.Pop();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DrawPiece(PieceType[0], new Vector2I(5, 1), new Vector2I(1, 0));
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
			SetCell(ACTIVE_LAYER, position + part, TileId, atlas);
		}
	}
}
