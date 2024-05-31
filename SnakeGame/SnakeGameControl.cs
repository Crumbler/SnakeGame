using CustomTimers;
using GameLogic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SnakeGame;

public sealed class SnakeGameControl : UIElement
{
    private const double CellSize = 10;
    private const int MillisecondPerGameTick = 300;

    private double desiredFramerate = 60;

    private static readonly Typeface messageTypeface =
        new(new FontFamily("Verdana"), FontStyles.Normal,
            FontWeights.DemiBold, FontStretches.Normal);

    private static readonly FormattedText gameNotRunningText = new(
        "Press Space\nto start",
        CultureInfo.InvariantCulture, FlowDirection.LeftToRight, 
        messageTypeface, 20, Brushes.Black, 1.0)
        {
            TextAlignment = TextAlignment.Center
        },
        gameOverText = new(
        "Game over\nPress R\nto Restart",
        CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
        messageTypeface, 20, Brushes.Black, 1.0)
        {
            TextAlignment = TextAlignment.Center
        };

    private readonly DispatcherTimer renderTimer = new(DispatcherPriority.Send);
    private readonly MultimediaTimer gameTimer = new()
    {
        Interval = 16
    };
    private readonly Stopwatch stopwatch = new();
    private TimeSpan elapsedTime;

    private int horizontalSize = 10;
    private int verticalSize = 10;

    private Game game;

    public Brush BackgroundBrush
    {
        get; set;
    } = new SolidColorBrush();

    public Brush SnakeBrush
    {
        get; set;
    } = Brushes.Green;

    public Brush FoodBrush
    {
        get; set;
    } = Brushes.Red;

    public double DesiredFramerate
    {
        get => desiredFramerate;
        set
        {
            desiredFramerate = value;
            RenderTimerFrequency = desiredFramerate;
        }
    }

    private double RenderTimerFrequency
    {
        set
        {
            renderTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / value - 5.0);
        }
    }

    public int HorizontalSize
    {
        get => horizontalSize;
        set
        {
            horizontalSize = value;
        }
    }

    public int VerticalSize
    {
        get => verticalSize;
        set
        {
            verticalSize = value;
        }
    }

    public SnakeGameControl() : base()
    {
        Focusable = true;
        ClipToBounds = true;
        
        RenderTimerFrequency = DesiredFramerate;
        
        KeyDown += SnakeGameControl_KeyDown;

        gameTimer.Elapsed += OnGameTimer;

        game = null!;
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);

        Initialize();
    }

    private void Initialize()
    {
        gameNotRunningText.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        game = new Game(HorizontalSize, VerticalSize);

        renderTimer.Tick += OnRenderTimer;
    }

    private void StartGame()
    {
        game.Start();

        renderTimer.Start();
        stopwatch.Start();
        gameTimer.Start();
        elapsedTime = TimeSpan.Zero;
    }

    private void RestartGame()
    {
        game.Reset();

        renderTimer.Stop();
        stopwatch.Stop();

        if (gameTimer.IsRunning)
        {
            gameTimer.Stop();
        }

        InvalidateVisual();
    }

    private void SnakeGameControl_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                if (game.State == GameState.NotStarted)
                {
                    StartGame();
                }
                break;

            case Key.R:
                RestartGame();
                break;

            case Key.A:
                SetSnakeDirection(Direction.Left);
                break;

            case Key.D:
                SetSnakeDirection(Direction.Right);
                break;

            case Key.W:
                SetSnakeDirection(Direction.Up);
                break;

            case Key.S:
                SetSnakeDirection(Direction.Down);
                break;
        }
    }

    private void SetSnakeDirection(Direction direction)
    {
        game.SnakeDirection = direction;
    }

    private void OnRenderTimer(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    private void OnGameTimer(object? sender, EventArgs e)
    {
        elapsedTime += stopwatch.Elapsed;
        stopwatch.Restart();

        while (elapsedTime.Milliseconds >= MillisecondPerGameTick && game.State != GameState.Over)
        {
            elapsedTime -= TimeSpan.FromMilliseconds(MillisecondPerGameTick);
            game.Advance();
        }

        if (game.State == GameState.Over)
        {
            gameTimer.Stop();
        }
    }

    protected override void OnRender(DrawingContext context)
    {
        DrawBackground(context);

        switch (game.State)
        {
            case GameState.NotStarted:
                DrawGameNotStarted(context);
                break;

            case GameState.Running:
                DrawGame(context);
                break;

            case GameState.Over:
                DrawGameOver(context);
                break;
        }
    }

    private void DrawBackground(DrawingContext context)
    {
        context.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
    }

    private void DrawGameNotStarted(DrawingContext context)
    {
        DrawCenteredText(context, gameNotRunningText);
    }

    private void DrawCenteredText(DrawingContext context, FormattedText text)
    {
        var textPoint = (Point)RenderSize;
        textPoint.X /= 2.0;
        textPoint.Y = (textPoint.Y - text.Height) / 2.0;

        context.DrawText(text, textPoint);
    }

    private void DrawGame(DrawingContext context)
    {
        for (int i = 0; i < game.SnakeParts.Count; ++i)
        {
            var part = game.SnakeParts[i];

            var cellPoint = new Point(part.x * CellSize, part.y * CellSize);

            context.DrawRectangle(SnakeBrush, null,
                new Rect(cellPoint, new Size(CellSize, CellSize)));
        }

        var foodPoint = new Point(game.FoodPosition.x * CellSize, game.FoodPosition.y * CellSize);
        context.DrawRectangle(FoodBrush, null,
            new Rect(foodPoint, new Size(CellSize, CellSize)));
    }

    private void DrawGameOver(DrawingContext context)
    {
        DrawGame(context);
        DrawCenteredText(context, gameOverText);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
    }

    protected override Size MeasureCore(Size availableSize) =>
        new(horizontalSize * CellSize, verticalSize * CellSize);
}
