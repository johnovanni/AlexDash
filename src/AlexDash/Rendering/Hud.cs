using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlexDash.Game;

namespace AlexDash.Rendering;

/// <summary>The bar above the field: level, clock, score and remaining Alexes.</summary>
public sealed class Hud(TextBlock levelText, TextBlock timeText, TextBlock scoreText, Panel livesPanel)
{
    private int _livesShown = -1;

    public void Update(GameSession session)
    {
        levelText.Text = $"Level {session.Level}";
        int secs = (int)Math.Ceiling(session.TimeLeft);
        timeText.Text = $"Time {secs}";
        timeText.Foreground = secs <= 5 ? Brushes.OrangeRed : Brushes.White;
        scoreText.Text = $"Score {session.Score}";

        if (_livesShown != session.Lives)
        {
            _livesShown = session.Lives;
            livesPanel.Children.Clear();
            for (int i = 0; i < session.Lives; i++)
            {
                var icon = Sprites.CreateAlex(AlexKind.Player).Root;
                icon.LayoutTransform = new ScaleTransform(0.7, 0.7);
                icon.Margin = new Thickness(2, 0, 2, 0);
                livesPanel.Children.Add(icon);
            }
        }
    }
}
