using Godot;
using System;
using System.Collections.Generic;
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

    public static readonly Vector2I[] o_0 = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
    public static readonly Vector2I[] o_90 = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
    public static readonly Vector2I[] o_180 = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];
    public static readonly Vector2I[] o_270 = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];

    public static readonly Vector2I[][] o = [o_0, o_90, o_180, o_270];

    public static readonly Vector2I[] z_0 = [new(0, 0), new(1, 0), new(1, 1), new(2, 1)];
    public static readonly Vector2I[] z_90 = [new(2, 0), new(1, 1), new(2, 1), new(1, 2)];
    public static readonly Vector2I[] z_180 = [new(0, 1), new(1, 1), new(1, 2), new(2, 2)];
    public static readonly Vector2I[] z_270 = [new(1, 0), new(0, 1), new(1, 1), new(0, 2)];

    public static readonly Vector2I[][] z = [z_0, z_90, z_180, z_270];

    public static readonly Vector2I[] s_0 = [new(1, 0), new(2, 0), new(0, 1), new(1, 1)];
    public static readonly Vector2I[] s_90 = [new(1, 0), new(1, 1), new(2, 1), new(2, 2)];
    public static readonly Vector2I[] s_180 = [new(1, 1), new(2, 1), new(0, 2), new(1, 2)];
    public static readonly Vector2I[] s_270 = [new(0, 0), new(0, 1), new(1, 1), new(1, 2)];

    public static readonly Vector2I[][] s = [s_0, s_90, s_180, s_270];

    public static readonly Vector2I[] l_0 = [new(2, 0), new(0, 1), new(1, 1), new(2, 1)];
    public static readonly Vector2I[] l_90 = [new(1, 0), new(1, 1), new(1, 2), new(2, 2)];
    public static readonly Vector2I[] l_180 = [new(0, 1), new(1, 1), new(2, 1), new(0, 2)];
    public static readonly Vector2I[] l_270 = [new(0, 0), new(1, 0), new(1, 1), new(1, 2)];
    public static readonly Vector2I[][] l = [l_0, l_90, l_180, l_270];

    public static readonly Vector2I[] j_0 = [new(0, 0), new(0, 1), new(1, 1), new(2, 1)];
    public static readonly Vector2I[] j_90 = [new(1, 0), new(2, 0), new(1, 1), new(1, 2)];
    public static readonly Vector2I[] j_180 = [new(0, 1), new(1, 1), new(2, 1), new(2, 2)];
    public static readonly Vector2I[] j_270 = [new(1, 0), new(1, 1), new(0, 2), new(1, 2)];
    public static readonly Vector2I[][] j = [j_0, j_90, j_180, j_270];

    public static readonly Vector2I[][][] AllShapes = [i, t, o, z, s, l, j];

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

    public const double INITIAL_DOWN_SPEED = 1.0;
    
    /// <summary>
    /// If the player holds left or right, this is the time measured in seconds it should take
    /// for the piece to move one grid square left or right after the first move (which should
    /// be registered on the next frame).
    /// </summary>
    public const double HORIZONTAL_INTERVAL = 0.08;
    public double horizontalSteps = 0;

    /// <summary>
    /// The speed is divided by this number on every completed row.
    /// </summary>
    public const double ACCELERATION = 1.1;
    /// <summary>
    /// The number of frames between movements - the lower the Interval, the faster the speed.
    /// </summary>
    public double Interval;

    /// <summary>
    /// This is a way to implement subpixels for moving the active piece down.
    /// </summary>
    public double downSteps = 0;

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

    public int Score = 0;
    private bool GameRunning = false;
    public const int REWARD = 100;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        NewGame();
        StartButton.Pressed += StartButton_Pressed;
    }

    private void StartButton_Pressed()
    {
        NewGame();
    }

    public Node Hud => FindChild("HUD", recursive: false);
    public Label GameOverLabel => Hud.GetNode("GameOverLabel") as Label;
    public Label ScoreLabel => Hud.GetNode("ScoreLabel") as Label;
    public Button StartButton => Hud.GetNode("StartButton") as Button;

    private void NewGame()
    {
        GameRunning = true;
        GameOverLabel.Hide();
        ClearBoard();
        ClearNextPanel();
        EraseActivePiece();

        // reset variables
        Interval = INITIAL_DOWN_SPEED;

        Score = 0;
        downSteps = 0;
        RenderScore();

        PieceType = PickPiece();
        // find the corresponding color in the piece atlas - since it's flat, the y is always 0.
        PieceAtlas = new Vector2I(Array.IndexOf(AllShapes, PieceType), 0);

        NextPieceType = PickPiece();
        NextPieceAtlas = new Vector2I(Array.IndexOf(AllShapes, NextPieceType), 0);

        CreatePiece();
    }

    private Vector2I[][] PickPiece()
    {
        if (next_shapes is not { Count: > 0 })
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
        downSteps = 0;
        horizontalSteps = 0;
        currentPosition = START_POSITION;
        activePiece = PieceType[rotationIndex];
        DrawPiece(activePiece, currentPosition, PieceAtlas);
        DrawPiece(NextPieceType[0], new Vector2I(15, 6), NextPieceAtlas);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (!GameRunning)
        {
            return;
        }

        if (Input.IsActionJustPressed("ui_up"))
        {
            TryRotatePiece();
        }

        if (Input.IsActionJustPressed("ui_left"))
        {
            TryMovePiece(Vector2I.Left);
            horizontalSteps = 0;
        }
        else if (Input.IsActionPressed("ui_left"))
        {
            
            horizontalSteps -= delta;
            if (horizontalSteps < -HORIZONTAL_INTERVAL)
            {
                TryMovePiece(Vector2I.Left);
                horizontalSteps = 0;
            }
        }
        else if (Input.IsActionJustPressed("ui_right"))
        {
            TryMovePiece(Vector2I.Right);
            horizontalSteps = 0;
        }
        else if (Input.IsActionPressed("ui_right"))
        {
            horizontalSteps += delta;
            if (horizontalSteps > HORIZONTAL_INTERVAL)
            {
                TryMovePiece(Vector2I.Right);
                horizontalSteps = 0;
            }
        }
        else
        {
            horizontalSteps = 0;
        }

        if (Input.IsActionPressed("ui_down"))
        {
            downSteps += delta * 20;
        }

        downSteps += delta;

        if (downSteps > Interval)
        {
            TryMovePiece(Vector2I.Down);
            downSteps = 0;
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
                CheckGameOver();
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

    private void ClearBoard()
    {
        for (var x = 0; x < COLS + 1; x++)
        {
            for (var y = 1; y < ROWS; y++)
            {
                EraseCell(BOARD_LAYER, new Vector2I(x + 1, y));
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
        if (activePiece is null)
        {
            return;
        }
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

    public bool IsPositionFree(Vector2I position)
    {
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
                ShiftRows(row);
                Score += REWARD;
                Interval /= ACCELERATION;
                RenderScore();
            }
            else
            {
                row -= 1;
            }
        }
    }

    /// <summary>
    /// Refreshes the score text.
    /// </summary>
    private void RenderScore()
    {
        ScoreLabel.Text = $"Score: {Score}";
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

    public void CheckGameOver()
    {
        for (var i = 0; i < activePiece.Length; i++)
        {
            if (!IsPositionFree(activePiece[i] + currentPosition))
            {
                LandPiece();
                GameOverLabel.Show();
                GameRunning = false;
            }
        }
    }
}
