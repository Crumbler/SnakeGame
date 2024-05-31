using System.Runtime.InteropServices;

namespace GameLogic;

public sealed class Game
{
    public GameState State
    {
        get;
        private set;
    }

    public Direction SnakeDirection
    {
        get;
        set;
    }

    public Direction LastSnakeDirection
    {
        get;
        private set;
    }

    public (int y, int x) FoodPosition
    {
        get;
        private set;
    }

    public IReadOnlyCollection<(sbyte y, sbyte x)> SnakeParts => snakeParts;

    private readonly int horizontalSize, verticalSize;

    private readonly bool[,] field;
    private readonly List<(sbyte y, sbyte x)> snakeParts = [];
    private readonly Random random = new();

    public Game(int horizontalSize, int verticalSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(horizontalSize, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(verticalSize, 2);

        this.horizontalSize = horizontalSize;
        this.verticalSize = verticalSize;

        field = new bool[verticalSize, horizontalSize];

        ResetCore();
    }

    public void Start()
    {
        if (State != GameState.NotStarted)
        {
            throw new InvalidOperationException("Game already started");
        }

        State = GameState.Running;
    }

    public void Advance()
    {
        if (State != GameState.Running)
        {
            throw new InvalidOperationException("Game not running");
        }

        (sbyte y, sbyte x) tailPos = snakeParts[0];

        var newPos = tailPos;

        switch (SnakeDirection)
        {
            case Direction.Left:
                newPos.x--;
                break;

            case Direction.Top:
                newPos.y--;
                break;

            case Direction.Right:
                newPos.x++;
                break;

            case Direction.Bottom:
                newPos.y++;
                break;
        }

        if (newPos.x < 0 || newPos.y < 0 ||
            newPos.x >= horizontalSize || newPos.y >= verticalSize)
        {
            GameOver();
            return;
        }

        LastSnakeDirection = SnakeDirection;

        var span = CollectionsMarshal.AsSpan(snakeParts);

        span[..^1].CopyTo(span[1..]);
        snakeParts[^1] = newPos;

        if (newPos == FoodPosition)
        {
            snakeParts.Insert(0, tailPos);
            PlaceFood();
        }
    }

    private void GameOver()
    {
        State = GameState.Over;
    }

    public void Reset()
    {
        if (State == GameState.NotStarted)
        {
            return;
        }

        ResetCore();
    }

    private void ResetCore()
    {
        State = GameState.NotStarted;

        Array.Clear(field);

        snakeParts.Clear();
        snakeParts.Add((0, 0));

        field[0, 0] = true;

        SnakeDirection = Direction.Right;
        LastSnakeDirection = Direction.Right;

        PlaceFood();
    }

    private void PlaceFood()
    {
        FoodPosition = FindEmptyCell();
    }

    private (int y, int x) FindEmptyCell()
    {
        (int y, int x) pos;

        do
        {
            pos = (random.Next(verticalSize), random.Next(horizontalSize));
        }
        while (field[pos.y, pos.x]);

        return pos;
    }
}
