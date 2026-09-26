using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace AchievementTracker.Controls;

// A drifting, connecting-dot background inspired by skimmilkexe.dev's canvas particle effect.
// ponytail: naive O(n^2) neighbor check, fine at the capped particle count (<=70).
public class ParticleBackground : Control
{
    private const double LinkDistance = 140;
    private const int MaxParticles = 70;

    private readonly List<Particle> _particles = new();
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();

    private sealed class Particle
    {
        public double X, Y, Vx, Vy, R;
    }

    public ParticleBackground()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => Step();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Seed();
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Seed();
    }

    private void Seed()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var count = Math.Min(MaxParticles, (int)(w * h / 18000));
        _particles.Clear();
        for (var i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                X = _random.NextDouble() * w,
                Y = _random.NextDouble() * h,
                Vx = (_random.NextDouble() - 0.5) * 0.6,
                Vy = (_random.NextDouble() - 0.5) * 0.6,
                R = 1 + _random.NextDouble() * 1.6
            });
        }
    }

    private void Step()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        foreach (var p in _particles)
        {
            p.X += p.Vx;
            p.Y += p.Vy;
            if (p.X < 0 || p.X > w) p.Vx *= -1;
            if (p.Y < 0 || p.Y > h) p.Vy *= -1;
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var accent = TryGetResource("AppAccentBrush", ActualThemeVariant, out var res) && res is ISolidColorBrush brush
            ? brush.Color
            : Color.FromRgb(143, 192, 245);

        for (var i = 0; i < _particles.Count; i++)
        {
            var a = _particles[i];
            for (var j = i + 1; j < _particles.Count; j++)
            {
                var b = _particles[j];
                var dx = a.X - b.X;
                var dy = a.Y - b.Y;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < LinkDistance)
                {
                    var alpha = 0.16 * (1 - dist / LinkDistance);
                    var pen = new Pen(new SolidColorBrush(accent, alpha), 1);
                    context.DrawLine(pen, new Point(a.X, a.Y), new Point(b.X, b.Y));
                }
            }
        }

        var dotBrush = new SolidColorBrush(accent, 0.55);
        foreach (var p in _particles)
            context.DrawEllipse(dotBrush, null, new Point(p.X, p.Y), p.R, p.R);
    }
}
