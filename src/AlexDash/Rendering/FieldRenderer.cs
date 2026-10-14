using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlexDash.Game;

namespace AlexDash.Rendering;

/// <summary>
/// Draws a <see cref="World"/> on the play-field canvas: one visual per entity kept in sync with
/// the simulation, little walking/skating/button-mashing animations, flying controllers, and
/// speech bubbles.
/// </summary>
public sealed class FieldRenderer(Canvas field)
{
    private const double BubbleLife = 1.8;

    private sealed class AlexView
    {
        public required Sprites.AlexSprite Sprite;
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
    private readonly Dictionary<Projectile, FrameworkElement> _projectiles = [];
    private readonly List<Bubble> _bubbles = [];
    private readonly Random _rng = new();
    private World? _world;
    private double _time;

    public void Load(World world)
    {
        _world = world;
        field.Children.Clear();
        _alexes.Clear();
        _obstacles.Clear();
        _projectiles.Clear();
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
            var sprite = Sprites.CreateAlex(a.Kind);
            _alexes[a] = new AlexView { Sprite = sprite, AnimTime = _rng.NextDouble() * 10 };
            field.Children.Add(sprite.Root);
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
            var s = view.Sprite;
            view.AnimTime += dt;

            if (a.InBox != null)
            {
                // Headfirst in a crate, legs kicking.
                s.Tumble.Angle = 180 + Math.Sin(view.AnimTime * 18) * 10;
                s.Bob.X = s.Bob.Y = 0;
                s.Root.Opacity = 1;
                continue;
            }
            s.Tumble.Angle = 0;

            bool moving = Math.Abs(a.Vx) + Math.Abs(a.Vy) > 1;
            if (Math.Abs(a.Vx) > 1) s.Flip.ScaleX = a.Vx < 0 ? -1 : 1;
            else if (a.Kind == AlexKind.Gamer) s.Flip.ScaleX = a.FaceX;

            if (a.Kind == AlexKind.Gamer)
            {
                s.Controller!.Visibility = a.HasController ? Visibility.Visible : Visibility.Hidden;
                // Furious button-mashing, and a visible meltdown just before the throw.
                bool aboutToThrow = a.HasController && a.RageTimer < Tuning.RageWarning;
                s.Bob.X = aboutToThrow ? Math.Sin(view.AnimTime * 60) * 2 : 0;
                s.Bob.Y = a.HasController ? -Math.Abs(Math.Sin(view.AnimTime * 25)) * 0.8 : 0;
            }
            else
            {
                s.Bob.Y = !moving ? 0
                    : a.Kind == AlexKind.Skater ? Math.Sin(view.AnimTime * 4) * 1.5
                    : -Math.Abs(Math.Sin(view.AnimTime * (a.Kind == AlexKind.Pusher ? 7 : 14))) * 3;
            }

            // A dazed, flickering Alex while slowed.
            s.Root.Opacity = a.SlowTimer > 0 ? 0.55 + 0.45 * Math.Abs(Math.Sin(_time * 10)) : 1;
        }

        foreach (var (p, visual) in _projectiles)
        {
            if (!p.Landed && visual.RenderTransform is RotateTransform spin) spin.Angle += dt * 900;
        }
    }

    private void Sync()
    {
        foreach (var (o, visual) in _obstacles) Place(visual, o);
        foreach (var (a, view) in _alexes) Place(view.Sprite.Root, a);
        SyncProjectiles();
    }

    /// <summary>Controllers come and go mid-level, so their visuals are added and removed here.</summary>
    private void SyncProjectiles()
    {
        if (_world == null) return;

        foreach (var gone in _projectiles.Keys.Except(_world.Projectiles).ToList())
        {
            field.Children.Remove(_projectiles[gone]);
            _projectiles.Remove(gone);
        }

        foreach (var p in _world.Projectiles)
        {
            if (!_projectiles.TryGetValue(p, out var visual))
            {
                visual = Sprites.CreateController();
                visual.RenderTransform = new RotateTransform(0, p.W / 2, p.H / 2);
                _projectiles[p] = visual;
                field.Children.Add(visual);
            }
            Place(visual, p);
            if (p.Landed)
            {
                Panel.SetZIndex(visual, 50); // lying on the ground, under everyone
                visual.Opacity = Math.Clamp(p.LandedTimer / Tuning.ControllerLandedTime, 0, 1);
            }
            else
            {
                Panel.SetZIndex(visual, 5_000); // in the air, over everyone
            }
        }
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
