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

    public IReadOnlyList<(sbyte y, sbyte x)> SnakeParts => snakeParts;

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

        var newPos = snakeParts[^1];

        switch (SnakeDirection)
        {
            case Direction.Left:
                newPos.x--;
                break;

            case Direction.Up:
                newPos.y--;
                break;

            case Direction.Right:
                newPos.x++;
                break;

            case Direction.Down:
                newPos.y++;
                break;
        }

        field[tailPos.y, tailPos.x] = false;

        if (newPos.x < 0 || newPos.y < 0 ||
            newPos.x >= horizontalSize || newPos.y >= verticalSize ||
            field[newPos.y, newPos.x])
        {
            GameOver();
            return;
        }

        field[newPos.y, newPos.x] = true;
        
        LastSnakeDirection = SnakeDirection;

        var span = CollectionsMarshal.AsSpan(snakeParts);

        span[1..].CopyTo(span[..^1]);
        span[^1] = newPos;

        if (newPos == FoodPosition)
        {
            snakeParts.Insert(0, tailPos);
            field[tailPos.y, tailPos.x] = true;
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
        snakeParts.Add(((sbyte y, sbyte x))(verticalSize / 2, horizontalSize / 2));

        field[verticalSize / 2, horizontalSize / 2] = true;

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
