using static AlexDash.Game.Tuning;

namespace AlexDash.Game;

/// <summary>
/// Runs one level frame by frame: movement, obstacle hits, falling into crates, Alex-on-Alex bumps,
/// thrown controllers and the exit.
/// </summary>
public sealed class LevelSimulation(World world, Random rng)
{
    private const string StupidLine = "I'm not stupid, you stupid.";

    /// <summary>How much of a boxed Alex (its kicking legs) shows above the crate.</summary>
    private const double LegsShowing = 14;

    private static readonly (int dx, int dy)[] Sides = [(-1, 0), (1, 0), (0, -1), (0, 1)];

    private readonly NpcBehavior _npcs = new(world, rng);
    private readonly Dictionary<(int, int), double> _pairCooldowns = [];
    private double _time;

    public World World => world;

    public event Action<Speech>? Spoke;

    /// <param name="moveX">Player input, -1 (left) to 1 (right).</param>
    /// <param name="moveY">Player input, -1 (up) to 1 (down).</param>
    /// <returns>True once the player reaches the exit.</returns>
    public bool Step(double dt, double moveX, double moveY)
    {
        _time += dt;
        MovePlayer(dt, moveX, moveY);
        foreach (var npc in world.Npcs)
            if (npc.InBox == null) _npcs.Update(npc, dt);
        UpdateGamers(dt);
        UpdateProjectiles(dt);
        ResolvePlayerVsObstacles();
        UpdateBoxed(dt);
        CheckBoxFalls();
        ResolveAlexCollisions();

        var p = world.Player;
        return p.InBox == null && p.Bounds.Intersects(world.Exit);
    }

    // ───────────────────────── player ─────────────────────────

    private void MovePlayer(double dt, double dx, double dy)
    {
        var p = world.Player;
        if (p.InBox == null)
        {
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0) { dx /= len; dy /= len; }

            double speed = p.SlowTimer > 0 ? SlowedSpeed : PlayerSpeed;
            p.Vx = dx * speed;
            p.Vy = dy * speed;
            p.X += (p.Vx + p.KnockVx) * dt;
            p.Y += (p.Vy + p.KnockVy) * dt;
            Physics.ClampToField(p);
        }
        else
        {
            p.Vx = p.Vy = 0; // stuck in a crate: no steering
        }

        double decay = Math.Exp(-KnockbackDecay * dt);
        p.KnockVx *= decay;
        p.KnockVy *= decay;

        p.SlowTimer -= dt;
        p.HitImmunity -= dt;
    }

    private void ResolvePlayerVsObstacles()
    {
        var p = world.Player;
        if (p.InBox != null) return;

        bool hit = false;
        foreach (var o in world.Obstacles)
        {
            bool crate = o.Kind == ObstacleKind.Crate;
            if (crate && p.BoxImmunity <= 0) continue; // falls in instead; see CheckBoxFalls
            if (Physics.Overlap(p.Bounds, o.Bounds, out double dx, out double dy))
            {
                p.X += dx;
                p.Y += dy;
                if (!crate) hit = true;
            }
        }
        Physics.ClampToField(p);

        if (hit && p.HitImmunity <= 0)
        {
            p.SlowTimer = SlowDuration;
            p.HitImmunity = SlowDuration + HitGrace;
            Spoke?.Invoke(new Speech(p, "Twenny-won!", VoiceLine.TwennyWon, Interrupt: true));
        }
    }

    // ───────────────────────── crates ─────────────────────────

    private void CheckBoxFalls()
    {
        foreach (var a in world.Alexes)
        {
            if (a.InBox != null || a.Kind == AlexKind.Pusher || a.BoxImmunity > 0) continue;
            var crate = world.Obstacles.FirstOrDefault(o =>
                o.Kind == ObstacleKind.Crate && Physics.Overlap(a.Bounds, o.Bounds, out _, out _));
            if (crate != null) FallIn(a, crate);
        }
    }

    private void FallIn(Alex a, Obstacle crate)
    {
        a.InBox = crate;
        a.BoxTimer = BoxStuckTime;
        a.BoxEntryX = a.CenterX - crate.CenterX;
        a.BoxEntryY = a.CenterY - crate.CenterY;
        a.Vx = a.Vy = 0;
        a.KnockVx = a.KnockVy = 0;
        FollowBox(a, crate);
        Spoke?.Invoke(new Speech(a, "Waahh!", VoiceLine.Waah, Interrupt: a.IsPlayer));
    }

    private void UpdateBoxed(double dt)
    {
        foreach (var a in world.Alexes)
        {
            a.BoxImmunity -= dt;
            if (a.InBox is not { } crate) continue;

            a.BoxTimer -= dt;
            if (a.BoxTimer > 0) FollowBox(a, crate); // rides along if the crate is being pushed
            else ClimbOut(a, crate);
        }
    }

    /// <summary>Headfirst in the crate, with just the legs showing above it.</summary>
    private static void FollowBox(Alex a, Obstacle crate)
    {
        a.X = crate.CenterX - a.W / 2;
        a.Y = crate.Y - LegsShowing;
    }

    /// <summary>Climbs out on the side they fell in from, or the first other side with room.</summary>
    private void ClimbOut(Alex a, Obstacle crate)
    {
        a.InBox = null;
        a.BoxImmunity = BoxClimbOutGrace;

        (int dx, int dy) entry = Math.Abs(a.BoxEntryX) >= Math.Abs(a.BoxEntryY)
            ? (a.BoxEntryX < 0 ? -1 : 1, 0)
            : (0, a.BoxEntryY < 0 ? -1 : 1);

        var side = Sides.OrderBy(s => s == entry ? 0 : 1).FirstOrDefault(candidate =>
        {
            PlaceBeside(a, crate, candidate);
            return Physics.InField(a.Bounds)
                   && !world.Obstacles.Any(o => Physics.Overlap(a.Bounds, o.Bounds, out _, out _));
        }, entry);
        PlaceBeside(a, crate, side); // if every side was blocked, this squeezes out the way they came
        Physics.ClampToField(a);

        if (!a.IsPlayer) _npcs.Redirect(a, side.dx, side.dy); // wander off away from the crate
    }

    private static void PlaceBeside(Alex a, Obstacle crate, (int dx, int dy) side)
    {
        switch (side)
        {
            case (-1, 0): a.X = crate.X - a.W - 1; a.Y = crate.Y + crate.H - a.H; break;
            case (1, 0): a.X = crate.X + crate.W + 1; a.Y = crate.Y + crate.H - a.H; break;
            case (0, -1): a.X = crate.CenterX - a.W / 2; a.Y = crate.Y - a.H - 1; break;
            default: a.X = crate.CenterX - a.W / 2; a.Y = crate.Y + crate.H + 1; break;
        }
    }

    // ───────────────────────── gamers & controllers ─────────────────────────

    private void UpdateGamers(double dt)
    {
        foreach (var g in world.Npcs)
        {
            if (g.Kind != AlexKind.Gamer || g.InBox != null) continue;

            if (!g.HasController)
            {
                g.ReArmTimer -= dt;
                if (g.ReArmTimer <= 0) g.HasController = true; // pulls out a spare
                continue;
            }

            g.RageTimer -= dt;
            if (g.RageTimer <= 0) Throw(g);
        }
    }

    private void Throw(Alex gamer)
    {
        gamer.RageTimer = RageMin + rng.NextDouble() * (RageMax - RageMin);

        var targets = world.Alexes.Where(a => a != gamer && a.InBox == null).ToList();
        if (targets.Count == 0) return;
        var target = targets[rng.Next(targets.Count)];

        double angle = Math.Atan2(target.CenterY - gamer.CenterY, target.CenterX - gamer.CenterX)
                       + (rng.NextDouble() - 0.5) * 0.3; // not a perfect aim
        world.Projectiles.Add(new Projectile
        {
            Thrower = gamer,
            W = ControllerW, H = ControllerH,
            X = gamer.CenterX - ControllerW / 2,
            Y = gamer.CenterY - ControllerH / 2,
            Vx = Math.Cos(angle) * ControllerSpeed,
            Vy = Math.Sin(angle) * ControllerSpeed,
        });

        gamer.HasController = false;
        gamer.ReArmTimer = ControllerReArm;
        gamer.FaceX = Math.Cos(angle) < 0 ? -1 : 1;
        Spoke?.Invoke(new Speech(gamer, "Hackers!", VoiceLine.Hackers));
    }

    private void UpdateProjectiles(double dt)
    {
        for (int i = world.Projectiles.Count - 1; i >= 0; i--)
        {
            var p = world.Projectiles[i];
            if (p.Landed)
            {
                p.LandedTimer -= dt;
                if (p.LandedTimer <= 0) world.Projectiles.RemoveAt(i);
                continue;
            }

            p.X += p.Vx * dt;
            p.Y += p.Vy * dt;
            p.Traveled += ControllerSpeed * dt;

            var victim = world.Alexes.FirstOrDefault(a =>
                a != p.Thrower && a.InBox == null && p.Bounds.Intersects(a.Bounds));
            if (victim != null)
            {
                HitByController(victim, p);
                world.Projectiles.RemoveAt(i);
                continue;
            }

            bool blocked = !Physics.InField(p.Bounds) || p.Traveled > ControllerRange
                           || world.Obstacles.Any(o => p.Bounds.Intersects(o.Bounds));
            if (blocked)
            {
                Physics.ClampToField(p);
                p.Landed = true;
                p.LandedTimer = ControllerLandedTime;
            }
        }
    }

    private static void HitByController(Alex a, Projectile p)
    {
        double len = Math.Sqrt(p.Vx * p.Vx + p.Vy * p.Vy);
        a.KnockVx = p.Vx / len * ControllerKnockback;
        a.KnockVy = p.Vy / len * ControllerKnockback;
        a.SlowTimer = Math.Max(a.SlowTimer, ControllerHitSlow);
    }

    // ───────────────────────── Alex vs Alex ─────────────────────────

    private void ResolveAlexCollisions()
    {
        var all = world.Alexes.Where(a => a.InBox == null).ToList();
        for (int i = 0; i < all.Count; i++)
        for (int j = i + 1; j < all.Count; j++)
        {
            Alex a = all[i], b = all[j];
            if (!Physics.Overlap(a.Bounds, b.Bounds, out double dx, out double dy)) continue;

            // Push them apart (the player stands their ground and gets knocked back; NPCs get shoved).
            if (a.IsPlayer)
            {
                Physics.Shift(b, -dx, -dy);
                Physics.KnockBack(a, away: b);
            }
            else
            {
                Physics.Shift(a, dx / 2, dy / 2);
                Physics.Shift(b, -dx / 2, -dy / 2);
            }

            Bonk(a, dx, dy);
            Bonk(b, -dx, -dy);

            var key = (a.Id, b.Id);
            if (!_pairCooldowns.TryGetValue(key, out double readyAt) || _time >= readyAt)
            {
                _pairCooldowns[key] = _time + PairChatterCooldown;
                var speaker = rng.Next(2) == 0 ? a : b;
                var line = speaker.IsPlayer ? VoiceLine.NotStupid
                    : rng.Next(2) == 0 ? VoiceLine.NotStupidLow : VoiceLine.NotStupidHigh;
                Spoke?.Invoke(new Speech(speaker, StupidLine, line));
            }
        }
    }

    /// <summary>Slows a bumped Alex; NPCs also head off in a new direction, roughly away from (dx, dy).</summary>
    private void Bonk(Alex a, double dx, double dy)
    {
        a.SlowTimer = Math.Max(a.SlowTimer, BumpSlowDuration);
        if (!a.IsPlayer) _npcs.Redirect(a, dx, dy); // the player steers themselves
    }
}
