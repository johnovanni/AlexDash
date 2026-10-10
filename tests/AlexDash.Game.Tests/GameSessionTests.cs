namespace AlexDash.Game.Tests;

public class GameSessionTests
{
    private static GameSession StartedSession(int seed = 1)
    {
        var session = new GameSession(new Random(seed));
        session.Start();
        return session;
    }

    private static void PutPlayerOnExit(GameSession session)
    {
        var world = session.Simulation.World;
        world.Player.X = world.Exit.X;
        world.Player.Y = world.Exit.Y;
    }

    [Fact]
    public void Starts_on_the_title_screen_and_waits_for_Start()
    {
        var session = new GameSession(new Random(1));

        session.Update(1, 1, 0);

        Assert.Equal(GameState.Title, session.State);
        Assert.Equal(Tuning.TimeLimit(1), session.TimeLeft);
    }

    [Fact]
    public void Reaching_the_exit_scores_time_and_level_bonus_and_cries_out()
    {
        var session = StartedSession();
        var speech = new List<Speech>();
        session.Spoke += speech.Add;
        session.Update(2.2, 0, 0);
        PutPlayerOnExit(session);

        session.Update(0.01, 0, 0);

        Assert.Equal(GameState.LevelComplete, session.State);
        int secondsLeft = (int)Math.Ceiling(Tuning.TimeLimit(1) - 2.21);
        Assert.Equal(secondsLeft * 100 + 250, session.Score);
        Assert.Contains(speech, s => s.Line == VoiceLine.HawkCry && s.Interrupt && s.Speaker.IsPlayer);
    }

    [Fact]
    public void Next_level_starts_after_the_pause_with_a_shorter_clock()
    {
        var session = StartedSession();
        PutPlayerOnExit(session);
        session.Update(0.01, 0, 0);

        session.Update(Tuning.LevelEndPause, 0, 0);

        Assert.Equal(GameState.Playing, session.State);
        Assert.Equal(2, session.Level);
        Assert.Equal(Tuning.TimeLimit(2), session.TimeLeft);
        Assert.True(Tuning.TimeLimit(2) < Tuning.TimeLimit(1));
    }

    [Fact]
    public void Running_out_of_time_costs_an_Alex_and_repeats_the_level()
    {
        var session = StartedSession();

        session.Update(Tuning.TimeLimit(1) + 1, 0, 0);
        Assert.Equal(GameState.LevelFailed, session.State);
        Assert.Equal(Tuning.StartingLives - 1, session.Lives);

        session.Update(Tuning.LevelEndPause, 0, 0);
        Assert.Equal(GameState.Playing, session.State);
        Assert.Equal(1, session.Level);
        Assert.Equal(Tuning.TimeLimit(1), session.TimeLeft);
    }

    [Fact]
    public void Third_timeout_ends_the_game_and_Enter_starts_over()
    {
        var session = StartedSession();
        for (int i = 0; i < Tuning.StartingLives; i++)
        {
            session.Update(Tuning.TimeLimit(1) + 1, 0, 0);
            session.Update(Tuning.LevelEndPause, 0, 0);
        }

        Assert.Equal(GameState.GameOver, session.State);
        Assert.Equal(0, session.Lives);

        session.Start();
        Assert.Equal(GameState.Playing, session.State);
        Assert.Equal(Tuning.StartingLives, session.Lives);
        Assert.Equal(0, session.Score);
    }

    [Fact]
    public void Pausing_stops_the_clock()
    {
        var session = StartedSession();
        session.TogglePause();

        session.Update(5, 0, 0);

        Assert.Equal(GameState.Paused, session.State);
        Assert.Equal(Tuning.TimeLimit(1), session.TimeLeft);
    }
}
