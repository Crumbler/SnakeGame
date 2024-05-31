namespace GameLogic;

public sealed class Game
{
    public GameState State
    {
        get;
        private set;
    }

    private readonly int horizontalSize, verticalSize;

    private readonly bool[,] field;
   
    public Game(int horizontalSize, int verticalSize)
    {
        State = GameState.NotStarted;

        ArgumentOutOfRangeException.ThrowIfLessThan(horizontalSize, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(verticalSize, 2);

        this.horizontalSize = horizontalSize;
        this.verticalSize = verticalSize;

        field = new bool[verticalSize, horizontalSize];
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
        if (State == GameState.Running)
        {
            throw new InvalidOperationException("Game not running");
        }
    }

    public void Reset()
    {
        State = GameState.NotStarted;
    }

    public bool GetCell(int x, int y)
    {
        return field[y, x];
    }
}
