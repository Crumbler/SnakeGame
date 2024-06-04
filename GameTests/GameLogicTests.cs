using GameLogic;

namespace GameTests;

[Parallelizable(ParallelScope.All)]
public static class GameLogicTests
{
    [Test]
    public static void Test_GameCreation()
    {
        Game? game = null;

        Assert.DoesNotThrow(() => game = new Game(5, 5));

        Assert.That(game!.State, Is.EqualTo(GameState.NotStarted));
    }

    [Test]
    public static void Test_GameCreation_HorizontalSize_Exception()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Game(1, 5));
    }

    [Test]
    public static void Test_GameCreation_VerticalSize_Exception()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Game(5, 1));
    }

    [Test]
    public static void Test_GameNotStarted_Advance_Throws()
    {
        var game = new Game(2, 2);

        Assert.Throws<InvalidOperationException>(game.Advance);
    }

    [Test]
    public static void Test_GameStarted_Advances()
    {
        var game = new Game(2, 2)
        {
            SnakeDirection = Direction.Up
        };

        game.Start();

        Assert.Multiple(() =>
        {
            Assert.That(game.SnakeParts, Has.Count.EqualTo(1)
                .And.ItemAt(0).EqualTo((1, 1)));

            Assert.That(game.State, Is.EqualTo(GameState.Running));
        });

        game.Advance();

        Assert.Multiple(() =>
        {
            Assert.That(game.SnakeParts[0], Is.EqualTo((0, 1)));
            Assert.That(game.LastSnakeDirection, Is.EqualTo(Direction.Up));
        });
    }

    [Test]
    public static void Test_GameStarted_Start_Throws()
    {
        var game = new Game(2, 2);

        game.Start();

        Assert.Throws<InvalidOperationException>(game.Start);
    }

    [Test]
    public static void Test_RunIntoWall_GameOver()
    {
        var game = new Game(2, 2)
        {
            SnakeDirection = Direction.Down
        };

        game.Start();

        game.Advance();

        Assert.That(game.State, Is.EqualTo(GameState.Over));
    }

    [Test]
    public static void Test_RunIntoTail_GameOver()
    {
        var game = new Game(12, 12)
        {
            SnakeDirection = Direction.Up
        };

        game.Start();

        GameDebugHelper.SetFoodPosition(game, (5, 6));
        game.Advance();

        game.SnakeDirection = Direction.Right;
        GameDebugHelper.SetFoodPosition(game, (5, 7));
        game.Advance();

        GameDebugHelper.SetFoodPosition(game, (5, 8));
        game.Advance();

        game.SnakeDirection = Direction.Down;
        GameDebugHelper.SetFoodPosition(game, (6, 8));
        game.Advance();

        game.SnakeDirection = Direction.Left;
        game.Advance();

        Assert.That(game.State, Is.EqualTo(GameState.Running));

        game.SnakeDirection = Direction.Up;
        game.Advance();

        Assert.That(game.State, Is.EqualTo(GameState.Over));
    }

    [Test]
    public static void Test_GameOver_StartAdvance_Throw()
    {
        var game = GetOverGame();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(game.Start);
            Assert.Throws<InvalidOperationException>(game.Advance);
        });
    }

    [Test]
    public static void Test_Growth()
    {
        var game = new Game(2, 2)
        {
            SnakeDirection = Direction.Up
        };

        game.Start();

        Assert.Multiple(() =>
        {
            Assert.That(game.SnakeParts, Has.Count.EqualTo(1)
                .And.ItemAt(0).EqualTo((1, 1)));

            Assert.That(game.State, Is.EqualTo(GameState.Running));
        });

        GameDebugHelper.SetFoodPosition(game, (0, 1));

        game.Advance();

        Assert.That(game.SnakeParts, Has.Count.EqualTo(2));
    }

    private static Game GetOverGame()
    {
        var game = new Game(2, 2)
        {
            SnakeDirection = Direction.Down
        };

        game.Start();

        game.Advance();

        return game;
    }
}