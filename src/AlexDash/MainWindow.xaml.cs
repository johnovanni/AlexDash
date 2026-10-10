using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AlexDash.Audio;
using AlexDash.Game;
using AlexDash.Rendering;

namespace AlexDash;

/// <summary>Runs the frame loop, reads the keyboard, and wires the game to the screen and speakers.</summary>
public partial class MainWindow : Window
{
    private readonly HashSet<Key> _keys = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Voice _voice = new();
    private readonly GameSession _session = new();
    private readonly FieldRenderer _renderer;
    private readonly Hud _hud;
    private double _lastFrame;

    public MainWindow()
    {
        InitializeComponent();
        _renderer = new FieldRenderer(Field);
        _hud = new Hud(LevelText, TimeText, ScoreText, LivesPanel);

        _session.LevelLoaded += () => _renderer.Load(_session.Simulation.World);
        _session.StateChanged += UpdateOverlay;
        _session.Spoke += speech =>
        {
            _renderer.ShowBubble(speech.Speaker, speech.Text);
            _voice.Play(speech.Line, speech.Interrupt);
        };

        KeyDown += OnKeyDown;
        KeyUp += (_, e) => _keys.Remove(e.Key);
        Deactivated += (_, _) => _keys.Clear();
        Closed += (_, _) => _voice.Dispose();
        CompositionTarget.Rendering += OnFrame;

        _renderer.Load(_session.Simulation.World);
        _hud.Update(_session);
        UpdateOverlay();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        _keys.Add(e.Key);
        switch (e.Key)
        {
            case Key.Enter:
                _session.Start();
                break;
            case Key.P:
                _session.TogglePause();
                break;
            case Key.K: // audition the exit cry without finishing a level
                _voice.Play(VoiceLine.HawkCry, interrupt: true);
                break;
        }
        e.Handled = true;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.05, now - _lastFrame);
        _lastFrame = now;
        if (dt <= 0) return;

        var (moveX, moveY) = ReadMovement();
        _session.Update(dt, moveX, moveY);

        if (_session.State is GameState.Playing or GameState.LevelComplete or GameState.LevelFailed)
            _renderer.Update(dt, animate: _session.State == GameState.Playing);
        _hud.Update(_session);
    }

    private (double x, double y) ReadMovement()
    {
        double x = 0, y = 0;
        if (Held(Key.Left, Key.A)) x -= 1;
        if (Held(Key.Right, Key.D)) x += 1;
        if (Held(Key.Up, Key.W)) y -= 1;
        if (Held(Key.Down, Key.S)) y += 1;
        return (x, y);
    }

    private bool Held(params Key[] keys) => keys.Any(_keys.Contains);

    private void UpdateOverlay()
    {
        switch (_session.State)
        {
            case GameState.Title:
                ShowOverlay("ALEX DASH",
                    "Get your Alex to the EXIT before the clock runs out!\n" +
                    "Arrow keys / WASD to move  ·  P to pause\n" +
                    "Bump into stuff and you'll slow down. Twenny-won!\n" +
                    "You get 3 Alexes. Faster exits = more points.\n\n" +
                    "Press ENTER to start");
                break;
            case GameState.Playing:
                Overlay.Visibility = Visibility.Collapsed;
                break;
            case GameState.Paused:
                ShowOverlay("PAUSED", "Press P to keep going");
                break;
            case GameState.LevelComplete:
                var r = _session.LastResult!;
                ShowOverlay("KAAAW!",
                    $"Level {r.Level} cleared with {r.SecondsLeft}s to spare!\n" +
                    $"Time bonus: {r.TimeBonus}   Level bonus: {r.LevelBonus}\n" +
                    $"Score: {_session.Score}");
                break;
            case GameState.LevelFailed:
                ShowOverlay("TIME'S UP!",
                    $"{_session.Lives} {(_session.Lives == 1 ? "Alex" : "Alexes")} left.\n" +
                    $"Try level {_session.Level} again...");
                break;
            case GameState.GameOver:
                ShowOverlay("GAME OVER",
                    $"All the Alexes are used up!\n" +
                    $"Final score: {_session.Score}   ·   Reached level {_session.Level}\n\n" +
                    "Press ENTER to play again");
                break;
        }
    }

    private void ShowOverlay(string title, string text)
    {
        OverlayTitle.Text = title;
        OverlayText.Text = text;
        Overlay.Visibility = Visibility.Visible;
    }
}
