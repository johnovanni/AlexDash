namespace AlexDash.Game;

public enum GameState { Title, Playing, Paused, LevelComplete, LevelFailed, GameOver }

public sealed record LevelResult(int Level, int SecondsLeft, int TimeBonus, int LevelBonus);

/// <summary>A whole game: level progression, the clock, lives and score.</summary>
public sealed class GameSession
{
    private readonly Random _rng;
    private double _stateTimer;

    public GameState State { get; private set; } = GameState.Title;
    public int Level { get; private set; } = 1;
    public int Lives { get; private set; } = Tuning.StartingLives;
    public int Score { get; private set; }
    public double TimeLeft { get; private set; }
    public LevelResult? LastResult { get; private set; }
    public LevelSimulation Simulation { get; private set; } = null!;

    /// <summary>An Alex said something.</summary>
    public event Action<Speech>? Spoke;

    /// <summary>A fresh level was built; <see cref="Simulation"/> has a new world.</summary>
    public event Action? LevelLoaded;

    public event Action? StateChanged;

    public GameSession(Random? rng = null)
    {
        _rng = rng ?? new Random();
        LoadLevel(); // something to look at behind the title screen
    }

    public void Start()
    {
        if (State is not (GameState.Title or GameState.GameOver)) return;
        Level = 1;
        Lives = Tuning.StartingLives;
        Score = 0;
        BeginLevel();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing) SetState(GameState.Paused);
        else if (State == GameState.Paused) SetState(GameState.Playing);
    }

    /// <param name="moveX">Player input, -1 (left) to 1 (right).</param>
    /// <param name="moveY">Player input, -1 (up) to 1 (down).</param>
    public void Update(double dt, double moveX, double moveY)
    {
        switch (State)
        {
            case GameState.Playing:
                TimeLeft -= dt;
                if (TimeLeft <= 0)
                {
                    TimeLeft = 0;
                    FailLevel();
                }
                else if (Simulation.Step(dt, moveX, moveY))
                {
                    CompleteLevel();
                }
                break;

            case GameState.LevelComplete:
            case GameState.LevelFailed:
                _stateTimer -= dt;
                if (_stateTimer <= 0)
                {
                    if (State == GameState.LevelComplete) Level++;
                    BeginLevel();
                }
                break;
        }
    }

    private void BeginLevel()
    {
        LoadLevel();
        SetState(GameState.Playing);
    }

    private void LoadLevel()
    {
        Simulation = new LevelSimulation(LevelBuilder.Build(Level, _rng), _rng);
        Simulation.Spoke += speech => Spoke?.Invoke(speech);
        TimeLeft = Tuning.TimeLimit(Level);
        LevelLoaded?.Invoke();
    }

    private void CompleteLevel()
    {
        int secondsLeft = (int)Math.Ceiling(TimeLeft);
        LastResult = new LevelResult(Level, secondsLeft, Tuning.TimeBonus(secondsLeft), Tuning.LevelBonus(Level));
        Score += LastResult.TimeBonus + LastResult.LevelBonus;

        Spoke?.Invoke(new Speech(Simulation.World.Player, "Kahww!", VoiceLine.HawkCry, Interrupt: true));
        _stateTimer = Tuning.LevelEndPause;
        SetState(GameState.LevelComplete);
    }

    private void FailLevel()
    {
        Lives--;
        _stateTimer = Tuning.LevelEndPause;
        SetState(Lives <= 0 ? GameState.GameOver : GameState.LevelFailed);
    }

    private void SetState(GameState state)
    {
        State = state;
        StateChanged?.Invoke();
    }
}
