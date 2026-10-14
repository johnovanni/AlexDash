using static AlexDash.Game.Tuning;

namespace AlexDash.Game;

/// <summary>
/// How the other Alexes move: runners zig-zag, skaters cruise diagonals, pushers shove crates,
/// and gamers stand still, absorbed in their game.
/// </summary>
public sealed class NpcBehavior(World world, Random rng)
{
    private static readonly (int dx, int dy)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    public void Update(Alex a, double dt)
    {
        a.TurnTimer -= dt;
        if (a.TurnTimer <= 0)
        {
            if (a.Kind == AlexKind.Pusher) TurnPusher(a);
            else PickNewDirection(a);
        }

        // Bumped Alexes stumble along at reduced speed for a bit.
        double moveDt = a.SlowTimer > 0 ? dt * BumpSlowFactor : dt;
        a.SlowTimer -= dt;

        // Knockback (from a thrown controller) carries them regardless of where they're heading.
        double kx = a.KnockVx * dt, ky = a.KnockVy * dt;
        double decay = Math.Exp(-KnockbackDecay * dt);
        a.KnockVx *= decay;
        a.KnockVy *= decay;

        if (a.Kind == AlexKind.Pusher)
        {
            MovePusher(a, a.Vx * moveDt + kx, a.Vy * moveDt + ky);
            return;
        }

        a.X += a.Vx * moveDt + kx;
        a.Y += a.Vy * moveDt + ky;
        Physics.BounceOffWalls(a);

        foreach (var o in world.Obstacles)
        {
            // Running into a crate means falling in (LevelSimulation handles that), unless they
            // just climbed out of one.
            if (o.Kind == ObstacleKind.Crate && a.BoxImmunity <= 0) continue;
            if (!Physics.Overlap(a.Bounds, o.Bounds, out double dx, out double dy)) continue;
            a.X += dx;
            a.Y += dy;
            Physics.Deflect(a, dx, dy);
        }
    }

    public void PickNewDirection(Alex a)
    {
        if (a.Kind == AlexKind.Gamer)
        {
            // Gamers don't wander; they're busy playing.
            a.Vx = a.Vy = 0;
            a.TurnTimer = double.PositiveInfinity;
            return;
        }

        double angle;
        if (a.Kind == AlexKind.Skater)
        {
            // Skaters cruise on long diagonals.
            angle = (45 + 90 * rng.Next(4) + (rng.NextDouble() - 0.5) * 30) * Math.PI / 180;
            a.TurnTimer = 2.5 + rng.NextDouble() * 3;
        }
        else
        {
            // Runners zig-zag around.
            angle = rng.NextDouble() * Math.PI * 2;
            a.TurnTimer = 0.7 + rng.NextDouble() * 1.5;
        }
        a.Vx = Math.Cos(angle) * a.Speed;
        a.Vy = Math.Sin(angle) * a.Speed;
    }

    /// <summary>Sends a bumped Alex off in a new direction, roughly away from (dx, dy).</summary>
    public void Redirect(Alex a, double dx, double dy)
    {
        if (a.Kind == AlexKind.Pusher)
        {
            TurnPusher(a);
            return;
        }

        double angle = Math.Atan2(dy, dx) + (rng.NextDouble() - 0.5) * Math.PI * 0.66;
        a.Vx = Math.Cos(angle) * a.Speed;
        a.Vy = Math.Sin(angle) * a.Speed;
        a.TurnTimer = Math.Max(a.TurnTimer, 1.0);
    }

    /// <summary>Points the pusher in a new direction with its crate in front. Returns false if boxed in.</summary>
    public bool TurnPusher(Alex a)
    {
        var crate = a.Crate!;
        double oldX = crate.X, oldY = crate.Y;
        var current = (Math.Sign(a.Vx), Math.Sign(a.Vy));

        foreach (var d in Directions.OrderBy(d => d == current ? 1 : 0).ThenBy(_ => rng.Next()))
        {
            PlaceCrate(a, d);
            if (PusherBlocked(a)) continue;
            a.Vx = d.dx * a.Speed;
            a.Vy = d.dy * a.Speed;
            a.TurnTimer = 3 + rng.NextDouble() * 3;
            return true;
        }

        crate.X = oldX;
        crate.Y = oldY;
        a.Vx = a.Vy = 0;
        a.TurnTimer = 0.5;
        return false;
    }

    private void MovePusher(Alex a, double mx, double my)
    {
        Physics.Shift(a, mx, my);

        if (PusherBlocked(a))
        {
            Physics.Shift(a, -mx, -my);
            a.KnockVx = a.KnockVy = 0;
            TurnPusher(a);
        }
    }

    private static void PlaceCrate(Alex a, (int dx, int dy) d)
    {
        var c = a.Crate!;
        switch (d)
        {
            case (1, 0): c.X = a.X + a.W + 1; c.Y = a.Y + a.H - c.H - 4; break;
            case (-1, 0): c.X = a.X - c.W - 1; c.Y = a.Y + a.H - c.H - 4; break;
            case (0, 1): c.X = a.X + (a.W - c.W) / 2; c.Y = a.Y + a.H + 1; break;
            default: c.X = a.X + (a.W - c.W) / 2; c.Y = a.Y - c.H - 1; break;
        }
    }

    private bool PusherBlocked(Alex a)
    {
        var crate = a.Crate!;
        var crateBounds = crate.Bounds;
        if (!Physics.InField(a.Bounds) || !Physics.InField(crateBounds)) return true;
        if (crateBounds.Intersects(world.ExitZone)) return true; // never shove a crate over the exit
        foreach (var o in world.Obstacles)
        {
            if (o == crate) continue;
            if (o.Bounds.Intersects(crateBounds) || o.Bounds.Intersects(a.Bounds)) return true;
        }
        return false;
    }
}
