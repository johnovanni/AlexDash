namespace AlexDash.Game.Tests;

public class CrateAndGamerTests
{
    private const double Frame = 0.016;

    private static World EmptyWorld(double playerX = 300, double playerY = 300)
    {
        var player = new Alex { Kind = AlexKind.Player, W = Tuning.AlexW, H = Tuning.AlexH, X = playerX, Y = playerY };
        return new World(player, new Box(Tuning.FieldW - 48, 20, 44, 80), new Box(0, 0, 1, 1));
    }

    private static Obstacle AddCrate(World world, double x, double y)
    {
        var crate = new Obstacle { Kind = ObstacleKind.Crate, X = x, Y = y, W = Tuning.CrateSize, H = Tuning.CrateSize };
        world.Obstacles.Add(crate);
        return crate;
    }

    private static Alex AddGamer(World world, double x, double y, double rageTimer)
    {
        var gamer = new Alex
        {
            Id = world.Npcs.Count + 1, Kind = AlexKind.Gamer, W = Tuning.AlexW, H = Tuning.AlexH,
            X = x, Y = y, HasController = true, RageTimer = rageTimer, TurnTimer = double.PositiveInfinity,
        };
        world.Npcs.Add(gamer);
        return gamer;
    }

    private static void Run(LevelSimulation sim, double seconds, double moveX = 0, double moveY = 0)
    {
        for (double t = 0; t < seconds; t += Frame) sim.Step(Frame, moveX, moveY);
    }

    [Fact]
    public void Walking_into_a_crate_tips_the_player_in_headfirst_crying()
    {
        var world = EmptyWorld();
        var crate = AddCrate(world, 360, 310);
        var sim = new LevelSimulation(world, new Random(1));
        var speech = new List<Speech>();
        sim.Spoke += speech.Add;

        Run(sim, 0.3, moveX: 1);

        Assert.Same(crate, world.Player.InBox);
        var waah = Assert.Single(speech);
        Assert.Equal(VoiceLine.Waah, waah.Line);
        Assert.True(waah.Interrupt);
        Assert.DoesNotContain(speech, s => s.Line == VoiceLine.TwennyWon);
    }

    [Fact]
    public void Player_cannot_move_while_stuck_and_climbs_out_the_way_they_came_after_a_second()
    {
        var world = EmptyWorld();
        var crate = AddCrate(world, 360, 310);
        var sim = new LevelSimulation(world, new Random(1));
        Run(sim, 0.3, moveX: 1);
        double stuckX = world.Player.X;

        Run(sim, 0.5, moveY: 1);
        Assert.NotNull(world.Player.InBox);
        Assert.Equal(stuckX, world.Player.X);

        Run(sim, 0.6);
        Assert.Null(world.Player.InBox);
        Assert.True(world.Player.X + world.Player.W <= crate.X, "climbed out on the left, where they fell in");
    }

    [Fact]
    public void Right_after_climbing_out_the_crate_is_solid()
    {
        var world = EmptyWorld();
        AddCrate(world, 360, 310);
        var sim = new LevelSimulation(world, new Random(1));
        int waahs = 0;
        sim.Spoke += s => { if (s.Line == VoiceLine.Waah) waahs++; };
        Run(sim, 1.4, moveX: 1); // fall in, climb out

        Run(sim, 0.3, moveX: 1); // walk straight back into it

        Assert.Null(world.Player.InBox);
        Assert.Equal(1, waahs);
    }

    [Fact]
    public void Other_Alexes_fall_in_too_and_head_away_afterwards()
    {
        var world = EmptyWorld(playerX: 20, playerY: 20);
        var crate = AddCrate(world, 560, 110);
        var runner = new Alex
        {
            Id = 1, Kind = AlexKind.Runner, W = Tuning.AlexW, H = Tuning.AlexH,
            X = 500, Y = 100, Vx = 150, Speed = 150, TurnTimer = 100,
        };
        world.Npcs.Add(runner);
        var sim = new LevelSimulation(world, new Random(1));
        var speech = new List<Speech>();
        sim.Spoke += speech.Add;

        Run(sim, 0.3);
        Assert.Same(crate, runner.InBox);
        Assert.Contains(speech, s => s.Speaker == runner && s.Line == VoiceLine.Waah && !s.Interrupt);

        Run(sim, 1.0);
        Assert.Null(runner.InBox);
        Assert.True(runner.Vx < 0, "heads back the way it came");
    }

    [Fact]
    public void Gamer_yells_Hackers_and_throws_the_controller()
    {
        var world = EmptyWorld();
        var gamer = AddGamer(world, 500, 300, rageTimer: 0.01);
        var sim = new LevelSimulation(world, new Random(1));
        var speech = new List<Speech>();
        sim.Spoke += speech.Add;

        sim.Step(Frame, 0, 0);

        var shout = Assert.Single(speech);
        Assert.Equal((VoiceLine.Hackers, gamer), (shout.Line, shout.Speaker));
        Assert.False(gamer.HasController);
        var controller = Assert.Single(world.Projectiles);
        Assert.True(controller.Vx < 0, "thrown toward the player on the left");
        Assert.Equal(-1, gamer.FaceX);
    }

    [Fact]
    public void Controller_hit_knocks_an_Alex_back_and_slows_them_for_a_second()
    {
        var world = EmptyWorld();
        AddGamer(world, 500, 300, rageTimer: 0.01);
        var sim = new LevelSimulation(world, new Random(1));

        for (int i = 0; i < 100 && world.Player.SlowTimer <= 0; i++) sim.Step(Frame, 0, 0);

        Assert.InRange(world.Player.SlowTimer, 0.9, Tuning.ControllerHitSlow);
        Assert.True(world.Player.KnockVx < 0, "knocked away from the gamer");
        Assert.Empty(world.Projectiles);
    }

    [Fact]
    public void Controller_lands_when_it_hits_an_obstacle_and_then_disappears()
    {
        var world = EmptyWorld();
        world.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Rock, X = 390, Y = 260, W = 50, H = 120 });
        AddGamer(world, 500, 300, rageTimer: 0.01);
        var sim = new LevelSimulation(world, new Random(1));

        Run(sim, 0.4);
        var controller = Assert.Single(world.Projectiles);
        Assert.True(controller.Landed);
        Assert.Equal(0, world.Player.SlowTimer, precision: 0);

        Run(sim, Tuning.ControllerLandedTime);
        Assert.Empty(world.Projectiles);
    }

    [Fact]
    public void Gamer_gets_a_new_controller_after_a_moment()
    {
        var world = EmptyWorld(playerX: 20, playerY: 20);
        var gamer = AddGamer(world, 500, 300, rageTimer: 0.01);
        var sim = new LevelSimulation(world, new Random(1));
        sim.Step(Frame, 0, 0);
        Assert.False(gamer.HasController);

        Run(sim, Tuning.ControllerReArm + 0.1);

        Assert.True(gamer.HasController);
        Assert.InRange(gamer.RageTimer, 0, Tuning.RageMax);
    }

    [Fact]
    public void Levels_start_with_an_armed_gamer()
    {
        var world = LevelBuilder.Build(1, new Random(1));

        var gamer = Assert.Single(world.Npcs, n => n.Kind == AlexKind.Gamer);
        Assert.True(gamer.HasController);
        Assert.Equal(0, gamer.Vx);
    }
}
