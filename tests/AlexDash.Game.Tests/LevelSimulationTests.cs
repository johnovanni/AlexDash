namespace AlexDash.Game.Tests;

public class LevelSimulationTests
{
    /// <summary>An empty field with the player in the middle-left and the exit far away.</summary>
    private static World EmptyWorld()
    {
        var player = new Alex { Kind = AlexKind.Player, W = Tuning.AlexW, H = Tuning.AlexH, X = 300, Y = 300 };
        return new World(player, new Box(Tuning.FieldW - 48, 20, 44, 80), new Box(0, 0, 1, 1));
    }

    private static Alex Runner(int id, double x, double y, double vx) => new()
    {
        Id = id, Kind = AlexKind.Runner, W = Tuning.AlexW, H = Tuning.AlexH,
        X = x, Y = y, Vx = vx, Speed = Math.Abs(vx), TurnTimer = 100,
    };

    [Fact]
    public void Hitting_an_obstacle_slows_the_player_and_says_Twenny_won_once()
    {
        var world = EmptyWorld();
        world.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Rock, X = 320, Y = 300, W = 40, H = 40 });
        var sim = new LevelSimulation(world, new Random(1));
        var speech = new List<Speech>();
        sim.Spoke += speech.Add;

        sim.Step(0.016, 1, 0);
        sim.Step(0.016, 1, 0);

        Assert.Equal(Tuning.SlowDuration, world.Player.SlowTimer, precision: 1);
        var twenny = Assert.Single(speech);
        Assert.Equal(VoiceLine.TwennyWon, twenny.Line);
        Assert.True(twenny.Interrupt);
        Assert.False(world.Player.Bounds.Intersects(world.Obstacles[0].Bounds.Inflate(-1)), "player is pushed out");
    }

    [Fact]
    public void Slowed_player_moves_at_slowed_speed()
    {
        var world = EmptyWorld();
        var sim = new LevelSimulation(world, new Random(1));
        world.Player.SlowTimer = 2;

        sim.Step(0.1, 1, 0);

        Assert.Equal(300 + Tuning.SlowedSpeed * 0.1, world.Player.X, precision: 6);
    }

    [Fact]
    public void Colliding_Alexes_both_slow_down_and_head_apart()
    {
        var world = EmptyWorld();
        var left = Runner(1, 500, 100, vx: 150);
        var right = Runner(2, 525, 100, vx: -150);
        world.Npcs.AddRange([left, right]);
        var sim = new LevelSimulation(world, new Random(1));
        var speech = new List<Speech>();
        sim.Spoke += speech.Add;

        sim.Step(0.016, 0, 0);

        Assert.True(left.SlowTimer > 0 && right.SlowTimer > 0);
        Assert.True(left.Vx < 0, "left Alex turns back left");
        Assert.True(right.Vx > 0, "right Alex turns back right");
        var line = Assert.Single(speech);
        Assert.Contains(line.Line, new[] { VoiceLine.NotStupidHigh, VoiceLine.NotStupidLow });
    }

    [Fact]
    public void Bumping_an_Alex_knocks_the_player_back_and_slows_them()
    {
        var world = EmptyWorld();
        world.Npcs.Add(Runner(1, 320, 300, vx: 0));
        var sim = new LevelSimulation(world, new Random(1));

        sim.Step(0.016, 0, 0);

        Assert.True(world.Player.KnockVx < 0, "knocked away from the Alex on the right");
        Assert.True(world.Player.SlowTimer > 0);
    }

    [Fact]
    public void Same_pair_does_not_argue_again_until_the_cooldown_passes()
    {
        var world = EmptyWorld();
        var a = Runner(1, 500, 100, vx: 0);
        var b = Runner(2, 520, 100, vx: 0);
        world.Npcs.AddRange([a, b]);
        var sim = new LevelSimulation(world, new Random(1));
        int lines = 0;
        sim.Spoke += _ => lines++;

        sim.Step(0.016, 0, 0);
        a.X = b.X = 500; // shove them together again right away
        sim.Step(0.016, 0, 0);

        Assert.Equal(1, lines);
    }

    [Fact]
    public void Reports_reaching_the_exit()
    {
        var world = EmptyWorld();
        var sim = new LevelSimulation(world, new Random(1));
        Assert.False(sim.Step(0.016, 0, 0));

        world.Player.X = world.Exit.X;
        world.Player.Y = world.Exit.Y;

        Assert.True(sim.Step(0.016, 0, 0));
    }
}
