using static AlexDash.Game.Tuning;

namespace AlexDash.Game;

/// <summary>Runs one level frame by frame: movement, obstacle hits, Alex-on-Alex bumps and the exit.</summary>
public sealed class LevelSimulation(World world, Random rng)
{
    private const string StupidLine = "I'm not stupid, you stupid.";

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
        foreach (var npc in world.Npcs) _npcs.Update(npc, dt);
        ResolvePlayerVsObstacles();
        ResolveAlexCollisions();
        return world.Player.Bounds.Intersects(world.Exit);
    }

    private void MovePlayer(double dt, double dx, double dy)
    {
        var p = world.Player;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len > 0) { dx /= len; dy /= len; }

        double speed = p.SlowTimer > 0 ? SlowedSpeed : PlayerSpeed;
        p.Vx = dx * speed;
        p.Vy = dy * speed;
        p.X += (p.Vx + p.KnockVx) * dt;
        p.Y += (p.Vy + p.KnockVy) * dt;
        Physics.ClampToField(p);

        double decay = Math.Exp(-KnockbackDecay * dt);
        p.KnockVx *= decay;
        p.KnockVy *= decay;

        p.SlowTimer -= dt;
        p.HitImmunity -= dt;
    }

    private void ResolvePlayerVsObstacles()
    {
        var p = world.Player;
        bool hit = false;
        foreach (var o in world.Obstacles)
        {
            if (Physics.Overlap(p.Bounds, o.Bounds, out double dx, out double dy))
            {
                p.X += dx;
                p.Y += dy;
                hit = true;
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

    private void ResolveAlexCollisions()
    {
        var all = world.Alexes.ToList();
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
