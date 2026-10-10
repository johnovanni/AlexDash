using static AlexDash.Game.Tuning;

namespace AlexDash.Game;

/// <summary>Lays out a level: exit, player, obstacles and other Alexes. Harder levels get more of everything.</summary>
public static class LevelBuilder
{
    private static readonly ObstacleKind[] StaticKinds =
        [ObstacleKind.Cone, ObstacleKind.TrashCan, ObstacleKind.Bush, ObstacleKind.Rock];

    public static World Build(int level, Random rng)
    {
        // Exit on the right edge, at a random height.
        var exit = new Box(FieldW - 48, 30 + rng.NextDouble() * (FieldH - 140), 44, 80);

        // Player starts on the left.
        var player = new Alex { Kind = AlexKind.Player, W = AlexW, H = AlexH, X = 20 };
        player.Y = 60 + rng.NextDouble() * (FieldH - 166);
        var world = new World(player, exit, new Box(0, player.Y - 70, 170, player.H + 140));

        PlaceObstacles(world, Math.Min(22, 6 + 2 * level), rng);

        var behavior = new NpcBehavior(world, rng);
        double speedScale = 1 + 0.07 * (level - 1);
        SpawnNpcs(world, behavior, rng, AlexKind.Skater, Math.Min(5, 1 + level / 2), 250 * speedScale);
        SpawnNpcs(world, behavior, rng, AlexKind.Runner, Math.Min(6, 2 + (level - 1) / 2), 150 * speedScale);
        SpawnNpcs(world, behavior, rng, AlexKind.Pusher, Math.Min(4, 1 + (level - 1) / 2), 60 * speedScale);

        return world;
    }

    /// <summary>Static obstacles, spaced apart so there's always a way through.</summary>
    private static void PlaceObstacles(World world, int count, Random rng)
    {
        for (int placed = 0, tries = 0; placed < count && tries < 2000; tries++)
        {
            double w = 34 + rng.NextDouble() * 30, h = 34 + rng.NextDouble() * 30;
            var r = new Box(140 + rng.NextDouble() * (FieldW - 260 - w), rng.NextDouble() * (FieldH - h), w, h);
            var padded = r.Inflate(36);
            if (padded.Intersects(world.StartZone) || padded.Intersects(world.ExitZone)) continue;
            if (world.Obstacles.Any(o => padded.Intersects(o.Bounds))) continue;

            world.Obstacles.Add(new Obstacle
            {
                Kind = StaticKinds[rng.Next(StaticKinds.Length)],
                X = r.X, Y = r.Y, W = r.W, H = r.H,
            });
            placed++;
        }
    }

    private static void SpawnNpcs(World world, NpcBehavior behavior, Random rng, AlexKind kind, int count, double speed)
    {
        for (int placed = 0, tries = 0; placed < count && tries < 1000; tries++)
        {
            var alex = new Alex
            {
                Kind = kind, W = AlexW, H = AlexH, Speed = speed,
                X = 200 + rng.NextDouble() * (FieldW - 320),
                Y = rng.NextDouble() * (FieldH - AlexH),
            };

            var padded = alex.Bounds.Inflate(8);
            if (padded.Intersects(world.StartZone)) continue;
            if (world.Obstacles.Any(o => padded.Intersects(o.Bounds))) continue;
            if (world.Npcs.Any(n => padded.Intersects(n.Bounds))) continue;

            if (kind == AlexKind.Pusher)
            {
                alex.Crate = new Obstacle { Kind = ObstacleKind.Crate, W = CrateSize, H = CrateSize };
                if (!behavior.TurnPusher(alex)) continue;
                world.Obstacles.Add(alex.Crate);
            }
            else
            {
                behavior.PickNewDirection(alex);
            }

            alex.Id = world.Npcs.Count + 1; // the player is 0
            world.Npcs.Add(alex);
            placed++;
        }
    }
}
