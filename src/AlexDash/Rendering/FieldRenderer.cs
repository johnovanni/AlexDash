using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlexDash.Game;

namespace AlexDash.Rendering;

/// <summary>
/// Draws a <see cref="World"/> on the play-field canvas: one visual per entity kept in sync with
/// the simulation, little walking/skating animations, and speech bubbles.
/// </summary>
public sealed class FieldRenderer(Canvas field)
{
    private const double BubbleLife = 1.8;

    private sealed class AlexView
    {
        public required FrameworkElement Root;
        public required TranslateTransform Bob;
        public required ScaleTransform Flip;
        public double AnimTime;
    }

    private sealed class Bubble
    {
        public required Alex Owner;
        public required FrameworkElement Element;
        public double Life;
    }

    private readonly Dictionary<Alex, AlexView> _alexes = [];
    private readonly Dictionary<Obstacle, FrameworkElement> _obstacles = [];
    private readonly List<Bubble> _bubbles = [];
    private readonly Random _rng = new();
    private double _time;

    public void Load(World world)
    {
        field.Children.Clear();
        _alexes.Clear();
        _obstacles.Clear();
        _bubbles.Clear();

        var exit = Sprites.CreateExit(world.Exit.W, world.Exit.H);
        Canvas.SetLeft(exit, world.Exit.X);
        Canvas.SetTop(exit, world.Exit.Y);
        Panel.SetZIndex(exit, 1);
        field.Children.Add(exit);

        foreach (var o in world.Obstacles)
        {
            var visual = Sprites.CreateObstacle(o.Kind, o.W, o.H);
            _obstacles[o] = visual;
            field.Children.Add(visual);
        }

        foreach (var a in world.Alexes)
        {
            var root = Sprites.CreateAlex(a.Kind, out var bob, out var flip);
            _alexes[a] = new AlexView { Root = root, Bob = bob, Flip = flip, AnimTime = _rng.NextDouble() * 10 };
            field.Children.Add(root);
        }

        Sync();
    }

    /// <param name="animate">False freezes the Alexes in place (e.g. on the level-end screen).</param>
    public void Update(double dt, bool animate)
    {
        if (animate)
        {
            _time += dt;
            Animate(dt);
        }
        Sync();
        UpdateBubbles(dt);
    }

    public void ShowBubble(Alex who, string text)
    {
        var existing = _bubbles.FirstOrDefault(b => b.Owner == who);
        if (existing != null)
        {
            field.Children.Remove(existing.Element);
            _bubbles.Remove(existing);
        }

        var bubble = new Bubble { Owner = who, Element = Sprites.CreateBubble(text), Life = BubbleLife };
        Panel.SetZIndex(bubble.Element, 10_000);
        field.Children.Add(bubble.Element);
        _bubbles.Add(bubble);
        PositionBubble(bubble);
    }

    private void Animate(double dt)
    {
        foreach (var (a, view) in _alexes)
        {
            bool moving = Math.Abs(a.Vx) + Math.Abs(a.Vy) > 1;
            view.AnimTime += dt;

            if (Math.Abs(a.Vx) > 1) view.Flip.ScaleX = a.Vx < 0 ? -1 : 1;

            view.Bob.Y = !moving ? 0
                : a.Kind == AlexKind.Skater ? Math.Sin(view.AnimTime * 4) * 1.5
                : -Math.Abs(Math.Sin(view.AnimTime * (a.Kind == AlexKind.Pusher ? 7 : 14))) * 3;

            // A dazed, flickering Alex while slowed.
            view.Root.Opacity = a.SlowTimer > 0 ? 0.55 + 0.45 * Math.Abs(Math.Sin(_time * 10)) : 1;
        }
    }

    private void Sync()
    {
        foreach (var (o, visual) in _obstacles) Place(visual, o);
        foreach (var (a, view) in _alexes) Place(view.Root, a);
    }

    private static void Place(FrameworkElement visual, Entity e)
    {
        Canvas.SetLeft(visual, e.X);
        Canvas.SetTop(visual, e.Y);
        Panel.SetZIndex(visual, 100 + (int)(e.Y + e.H)); // y-sort so things lower on screen draw in front
    }

    private void UpdateBubbles(double dt)
    {
        for (int i = _bubbles.Count - 1; i >= 0; i--)
        {
            var b = _bubbles[i];
            b.Life -= dt;
            if (b.Life <= 0)
            {
                field.Children.Remove(b.Element);
                _bubbles.RemoveAt(i);
            }
            else
            {
                PositionBubble(b);
            }
        }
    }

    private static void PositionBubble(Bubble b)
    {
        b.Element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = b.Element.DesiredSize;
        double left = Math.Clamp(b.Owner.CenterX - size.Width / 2, 2, Tuning.FieldW - size.Width - 2);
        double top = Math.Max(2, b.Owner.Y - size.Height - 4);
        Canvas.SetLeft(b.Element, left);
        Canvas.SetTop(b.Element, top);
    }
}
