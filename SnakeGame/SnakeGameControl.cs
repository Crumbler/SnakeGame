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
    private const double CellSize = 10,
        SnakeWidth = CellSize * 0.7;
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

    private readonly TranslateTransform foodTranslateTransform = new();
    private readonly List<(TranslateTransform tr, RotateTransform rt)> transforms =
    [
        (new TranslateTransform(), new RotateTransform(0, CellSize / 2.0, CellSize / 2.0))
    ];

    public Brush BackgroundBrush
    {
        get; set;
    } = new SolidColorBrush();

    public Drawing? FoodDrawing
    {
        get; set;
    }

    public Brush SnakeBrush
    {
        get; set;
    } = Brushes.Green;

    private readonly Pen SnakePen = new(null, CellSize * 0.01);

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

    private static double DirectionToAngle(Direction direction) => direction switch
    {
        Direction.Down => 90,
        Direction.Left => 180,
        Direction.Up => 270,
        _ => 0
    };

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

        SnakePen.Brush = SnakeBrush;

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
        for (int i = game.SnakeParts.Count - transforms.Count; i > 0; --i)
        {
            transforms.Add((new TranslateTransform(),
                new RotateTransform(0, CellSize / 2.0, CellSize / 2.0)));
        }

        DrawSnake(context);

        DrawFood(context);
    }

    private void DrawFood(DrawingContext context)
    {
        TranslateToCell(context, foodTranslateTransform, ((sbyte y, sbyte x))game.FoodPosition);

        context.DrawDrawing(FoodDrawing);

        context.Pop();
    }

    private static void TranslateToCell(DrawingContext context, TranslateTransform transform,
        (sbyte y, sbyte x) cell)
    {
        transform.X = cell.x * CellSize;
        transform.Y = cell.y * CellSize;

        context.PushTransform(transform);
    }

    private static void RotateByDirection(DrawingContext context, RotateTransform transform,
        Direction direction)
    {
        transform.Angle = DirectionToAngle(direction);

        context.PushTransform(transform);
    }

    private static Direction GetDirection((sbyte y, sbyte x) from, (sbyte y, sbyte x) to)
    {
        if (from.x < to.x)
        {
            return Direction.Right;
        }

        if (from.x > to.x)
        {
            return Direction.Left;
        }

        if (from.y < to.y)
        {
            return Direction.Down;
        }

        return Direction.Up;
    }

    private static bool IsUp(Direction a, Direction b) => (a, b) switch
    {
        (Direction.Left, Direction.Up) => true,
        (Direction.Down, Direction.Left) => true,
        (Direction.Right, Direction.Down) => true,
        (Direction.Up, Direction.Right) => true,
        _ => false
    };

    private void DrawSnake(DrawingContext context)
    {
        TranslateToCell(context, transforms[0].tr, game.SnakeParts[0]);
        RotateByDirection(context, transforms[0].rt, game.LastSnakeDirection);

        if (game.SnakeParts.Count == 1)
        {
            DrawLoneSnakeHead(context);
        }
        else
        {
            DrawSnakeHead(context);
        }

        context.Pop();
        context.Pop();

        for (int i = 1; i < game.SnakeParts.Count - 1; ++i)
        {
            var part = game.SnakeParts[i];
            var prevPart = game.SnakeParts[i - 1];
            var nextPart = game.SnakeParts[i + 1];

            bool vertDiff = prevPart.y != nextPart.y;
            bool horDiff = prevPart.x != nextPart.x;

            TranslateToCell(context, transforms[i].tr, game.SnakeParts[i]);
            RotateByDirection(context, transforms[i].rt, GetDirection(part, prevPart));

            if (vertDiff ^ horDiff)
            {
                DrawSnakeTrunk(context);
            }
            else
            {
                bool isUp = IsUp(GetDirection(prevPart, part), GetDirection(part, nextPart));
                DrawSnakeTurn(context, isUp);
            }

            context.Pop();
            context.Pop();
        }

        if (game.SnakeParts.Count > 1)
        {
            TranslateToCell(context, transforms[^1].tr, game.SnakeParts[^1]);

            RotateByDirection(context, transforms[^1].rt, 
                GetDirection(game.SnakeParts[^1], game.SnakeParts[^2]));

            DrawSnakeTail(context);

            context.Pop();
            context.Pop();
        }
    }

    private void DrawSnakeTrunk(DrawingContext context)
    {
        context.DrawRectangle(SnakeBrush, SnakePen,
                    new Rect(0, (CellSize - SnakeWidth) / 2.0,
                        CellSize, SnakeWidth));
    }

    private void DrawSnakeTurn(DrawingContext context, bool isUp)
    {
        const double bodyX = (CellSize - SnakeWidth) / 2.0,
            bodyY = bodyX,
            bodyLength = CellSize - bodyX,
            turnHeight = bodyLength - SnakeWidth;

        context.DrawRectangle(SnakeBrush, SnakePen,
            new Rect(bodyX, bodyY,
                bodyLength, SnakeWidth));

        double turnY = isUp ? 0 : bodyLength;

        context.DrawRectangle(SnakeBrush, SnakePen,
            new Rect(bodyX, turnY,
                SnakeWidth, turnHeight));
    }

    private void DrawSnakeTail(DrawingContext context)
    {
        const double bodyX = (CellSize - SnakeWidth) / 2.0,
            bodyY = bodyX,
            bodyLength = (CellSize - SnakeWidth) / 2.0 + SnakeWidth;

        context.DrawRectangle(SnakeBrush, SnakePen,
                    new Rect(bodyX, bodyY,
                        bodyLength, SnakeWidth));
    }

    private void DrawSnakeHead(DrawingContext context)
    {
        const double bodyX = 0,
            bodyY = (CellSize - SnakeWidth) / 2.0,
            bodyLength = (CellSize - SnakeWidth) / 2.0 + SnakeWidth;

        context.DrawRectangle(SnakeBrush, SnakePen,
                    new Rect(bodyX, bodyY,
                        bodyLength, SnakeWidth));

        const double eyeHeight = CellSize * 0.2,
            eyeWidth = CellSize * 0.25,
            eyeX = bodyX + bodyLength - eyeWidth - SnakeWidth * 0.1,
            topEyeY = bodyY + (SnakeWidth - eyeHeight * 2.0) / 3.0,
            bottomEyeY = bodyY + (SnakeWidth - eyeHeight * 2.0) * 2.0 / 3.0 + eyeHeight,
            pupilSize = eyeHeight / 2.0,
            pupilX = eyeX + (eyeWidth - pupilSize) / 2.0,
            topPupilY = topEyeY + (eyeHeight - pupilSize) / 2.0,
            bottomPupilY = bottomEyeY + (eyeHeight - pupilSize) / 2.0;

        context.DrawRectangle(Brushes.White, null,
            new Rect(eyeX, topEyeY,
                eyeWidth, eyeHeight));

        context.DrawRectangle(Brushes.White, null,
            new Rect(eyeX, bottomEyeY,
                eyeWidth, eyeHeight));

        context.DrawRectangle(Brushes.Black, null,
            new Rect(pupilX, topPupilY,
                pupilSize, pupilSize));

        context.DrawRectangle(Brushes.Black, null,
            new Rect(pupilX, bottomPupilY,
                pupilSize, pupilSize));
    }

    private void DrawLoneSnakeHead(DrawingContext context)
    {
        const double bodyX = (CellSize - SnakeWidth) / 2.0,
            bodyY = bodyX;

        context.DrawRectangle(SnakeBrush, SnakePen,
                    new Rect(bodyX, bodyY,
                        SnakeWidth, SnakeWidth));

        const double eyeHeight = CellSize * 0.2,
            eyeWidth = CellSize * 0.25,
            eyeX = bodyX + SnakeWidth - eyeWidth - SnakeWidth * 0.1,
            topEyeY = bodyY + (SnakeWidth - eyeHeight * 2.0) / 3.0,
            bottomEyeY = bodyY + (SnakeWidth - eyeHeight * 2.0) * 2.0 / 3.0 + eyeHeight,
            pupilSize = eyeHeight / 2.0,
            pupilX = eyeX + (eyeWidth - pupilSize) / 2.0,
            topPupilY = topEyeY + (eyeHeight - pupilSize) / 2.0,
            bottomPupilY = bottomEyeY + (eyeHeight - pupilSize) / 2.0;

        context.DrawRectangle(Brushes.White, null,
            new Rect(eyeX, topEyeY,
                eyeWidth, eyeHeight));

        context.DrawRectangle(Brushes.White, null,
            new Rect(eyeX, bottomEyeY,
                eyeWidth, eyeHeight));

        context.DrawRectangle(Brushes.Black, null,
            new Rect(pupilX, topPupilY,
                pupilSize, pupilSize));

        context.DrawRectangle(Brushes.Black, null,
            new Rect(pupilX, bottomPupilY,
                pupilSize, pupilSize));
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
