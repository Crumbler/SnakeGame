using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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

    private readonly DispatcherTimer renderTimer = new(DispatcherPriority.Send);
    private readonly Stopwatch stopwatch = new();

    private int horizontalSize = 10;
    private int verticalSize = 10;

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
    }

    protected override void OnVisualParentChanged(DependencyObject oldParent)
    {
        base.OnVisualParentChanged(oldParent);

        Initialize();
    }

    private void Initialize()
    {
        renderTimer.Tick += OnRenderTimer;
        stopwatch.Start();
        renderTimer.Start();
    }

    private void SnakeGameControl_KeyDown(object sender, KeyEventArgs e)
    {
        
    }

    private void OnRenderTimer(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext context)
    {
        DrawBackground(context);

        double x = Math.Sin(stopwatch.Elapsed.TotalMilliseconds / 500.0) * 50.0 + 50.0;
        context.DrawLine(pen, new Point(0, 0), new Point(x, 100));

    }

    private void DrawBackground(DrawingContext context)
    {
        context.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
    }

    protected override Size MeasureCore(Size availableSize) =>
        new(horizontalSize * CellSize, verticalSize * CellSize);
}
