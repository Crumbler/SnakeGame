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

    private readonly Pen pen = new(Brushes.Blue, 5.0);

    private double desiredFramerate = 60;

    private static readonly Typeface messageTypeface =
        new(new FontFamily("Verdana"), FontStyles.Normal,
            FontWeights.DemiBold, FontStretches.Normal);

    private static readonly FormattedText gameNotRunningText = new(
        "Press Space \nto start",
        CultureInfo.InvariantCulture, FlowDirection.LeftToRight, 
        messageTypeface, 20, Brushes.Black, 1.0);

    private readonly DispatcherTimer renderTimer = new(DispatcherPriority.Send);
    private readonly Stopwatch stopwatch = new();

    private int horizontalSize = 10;
    private int verticalSize = 10;

    private Game game;

    public Brush BackgroundBrush
    {
        get; set;
    } = new SolidColorBrush();

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
        gameNotRunningText.TextAlignment = TextAlignment.Center;

        game = new Game(HorizontalSize, VerticalSize);

        renderTimer.Tick += OnRenderTimer;
    }

    private void StartGame()
    {
        game.Start();

        stopwatch.Start();
        renderTimer.Start();
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
        }
    }

    private void OnRenderTimer(object? sender, EventArgs e)
    {
        InvalidateVisual();
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
                DrawGameRunning(context);
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

    private void DrawGameRunning(DrawingContext context)
    {
        double x = Math.Sin(stopwatch.Elapsed.TotalMilliseconds / 500.0) * 50.0 + 50.0;
        context.DrawLine(pen, new Point(), new Point(x, 100));
    }

    private void DrawGameOver(DrawingContext context)
    {

    }

    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
    }

    protected override Size MeasureCore(Size availableSize) =>
        new(horizontalSize * CellSize, verticalSize * CellSize);
}
