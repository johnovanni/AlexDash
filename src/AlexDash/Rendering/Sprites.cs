using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using AlexDash.Game;

namespace AlexDash.Rendering;

/// <summary>Builds every visual in the game out of plain WPF shapes.</summary>
public static class Sprites
{
    // The Alex drawing is laid out on this exact grid, so it must match the game's hitbox.
    private const double AlexW = Tuning.AlexW, AlexH = Tuning.AlexH;

    private static readonly Brush Skin = B("#F2C29B");
    private static readonly Brush Hair = B("#6B3E1F");
    private static readonly Brush Jeans = B("#3B5BA5");
    private static readonly Brush Shoes = B("#263238");
    private static readonly Brush Dark = B("#212121");

    public static Canvas CreateAlex(AlexKind kind, out TranslateTransform bob, out ScaleTransform flip)
    {
        var root = new Canvas { Width = AlexW, Height = AlexH };
        var body = new Canvas { Width = AlexW, Height = AlexH };

        flip = new ScaleTransform(1, 1, AlexW / 2, AlexH / 2);
        bob = new TranslateTransform();
        var transforms = new TransformGroup();
        transforms.Children.Add(flip);
        transforms.Children.Add(bob);
        body.RenderTransform = transforms;

        var shirt = kind switch
        {
            AlexKind.Player => B("#E53935"),
            AlexKind.Skater => B("#1E88E5"),
            AlexKind.Runner => B("#FB8C00"),
            _ => B("#8E24AA"),
        };

        if (kind == AlexKind.Skater)
        {
            Put(body, new Rectangle { Width = 30, Height = 4, RadiusX = 2, RadiusY = 2, Fill = B("#C0392B") }, 0, 41);
            Put(body, new Ellipse { Width = 4, Height = 4, Fill = Dark }, 4, 43);
            Put(body, new Ellipse { Width = 4, Height = 4, Fill = Dark }, 22, 43);
        }

        // legs + shoes
        Put(body, new Rectangle { Width = 5, Height = 10, Fill = Jeans }, 9, 30);
        Put(body, new Rectangle { Width = 5, Height = 10, Fill = Jeans }, 16, 30);
        Put(body, new Rectangle { Width = 7, Height = 3, RadiusX = 1, RadiusY = 1, Fill = Shoes }, 8, 38);
        Put(body, new Rectangle { Width = 7, Height = 3, RadiusX = 1, RadiusY = 1, Fill = Shoes }, 16, 38);

        // back arm, torso, front arm
        Put(body, new Rectangle { Width = 4, Height = 11, RadiusX = 2, RadiusY = 2, Fill = shirt }, 4, 19);
        Put(body, new Rectangle { Width = 18, Height = 14, RadiusX = 4, RadiusY = 4, Fill = shirt }, 6, 18);
        if (kind == AlexKind.Pusher)
        {
            Put(body, new Rectangle { Width = 10, Height = 4, RadiusX = 2, RadiusY = 2, Fill = shirt }, 20, 21);
            Put(body, new Ellipse { Width = 5, Height = 5, Fill = Skin }, 28, 20);
        }
        else
        {
            Put(body, new Rectangle { Width = 4, Height = 11, RadiusX = 2, RadiusY = 2, Fill = shirt }, 22, 19);
        }

        // head, hair, face
        Put(body, new Ellipse { Width = 18, Height = 17, Fill = Skin }, 7, 2);
        Put(body, new Border { Width = 19, Height = 7, Background = Hair, CornerRadius = new CornerRadius(9, 9, 2, 2) }, 6, 1);
        Put(body, new Polygon { Points = Pts("6,6 9,10 11,6"), Fill = Hair }, 0, 0);
        Put(body, new Ellipse { Width = 3, Height = 3, Fill = Dark }, 14, 9);
        Put(body, new Ellipse { Width = 3, Height = 3, Fill = Dark }, 20, 9);
        Put(body, new Ellipse { Width = 6, Height = 3, Fill = B("#B71C1C") }, 15, 13);

        switch (kind)
        {
            case AlexKind.Player:
                Put(body, new Border { Width = 20, Height = 6, Background = B("#FFC107"), CornerRadius = new CornerRadius(9, 9, 0, 0) }, 6, 0);
                Put(body, new Rectangle { Width = 9, Height = 2.5, Fill = B("#FFA000") }, 21, 5);
                body.Effect = new DropShadowEffect { Color = Colors.Yellow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.9 };
                break;
            case AlexKind.Runner:
                Put(body, new Rectangle { Width = 18, Height = 2, Fill = B("#D32F2F") }, 7, 6);
                break;
            case AlexKind.Skater:
                Put(body, new Border { Width = 20, Height = 7, Background = B("#43A047"), CornerRadius = new CornerRadius(9, 9, 1, 1) }, 6, 0);
                break;
        }

        root.Children.Add(body);
        return root;
    }

    public static FrameworkElement CreateObstacle(ObstacleKind kind, double w, double h)
    {
        var c = new Canvas { Width = 40, Height = 40 };
        switch (kind)
        {
            case ObstacleKind.Cone:
                Put(c, new Polygon { Points = Pts("20,2 32,36 8,36"), Fill = B("#FF6D00"), Stroke = B("#BF360C"), StrokeThickness = 1 }, 0, 0);
                Put(c, new Polygon { Points = Pts("15,17 25,17 27,24 13,24"), Fill = Brushes.White }, 0, 0);
                Put(c, new Rectangle { Width = 36, Height = 4, Fill = B("#E65100") }, 2, 35);
                break;
            case ObstacleKind.TrashCan:
                Put(c, new Rectangle { Width = 26, Height = 30, RadiusX = 3, RadiusY = 3, Fill = B("#78909C"), Stroke = B("#455A64"), StrokeThickness = 1.5 }, 7, 9);
                Put(c, new Rectangle { Width = 32, Height = 5, RadiusX = 2, RadiusY = 2, Fill = B("#546E7A") }, 4, 5);
                Put(c, new Rectangle { Width = 2, Height = 22, Fill = B("#546E7A") }, 14, 13);
                Put(c, new Rectangle { Width = 2, Height = 22, Fill = B("#546E7A") }, 24, 13);
                break;
            case ObstacleKind.Bush:
                Put(c, new Ellipse { Width = 22, Height = 22, Fill = B("#2E7D32") }, 1, 13);
                Put(c, new Ellipse { Width = 22, Height = 22, Fill = B("#388E3C") }, 17, 13);
                Put(c, new Ellipse { Width = 24, Height = 24, Fill = B("#43A047") }, 8, 2);
                Put(c, new Ellipse { Width = 4, Height = 4, Fill = B("#E53935") }, 14, 10);
                Put(c, new Ellipse { Width = 4, Height = 4, Fill = B("#E53935") }, 26, 22);
                break;
            case ObstacleKind.Rock:
                Put(c, new Ellipse { Width = 38, Height = 30, Fill = B("#9E9E9E"), Stroke = B("#616161"), StrokeThickness = 2 }, 1, 8);
                Put(c, new Ellipse { Width = 10, Height = 6, Fill = B("#BDBDBD") }, 9, 13);
                break;
            case ObstacleKind.Crate:
                Put(c, new Rectangle { Width = 38, Height = 38, Fill = B("#A1662F"), Stroke = B("#5D4037"), StrokeThickness = 3 }, 1, 1);
                Put(c, new Line { X1 = 3, Y1 = 3, X2 = 37, Y2 = 37, Stroke = B("#5D4037"), StrokeThickness = 3 }, 0, 0);
                Put(c, new Line { X1 = 37, Y1 = 3, X2 = 3, Y2 = 37, Stroke = B("#5D4037"), StrokeThickness = 3 }, 0, 0);
                break;
        }
        return new Viewbox { Width = w, Height = h, Stretch = Stretch.Fill, Child = c };
    }

    public static FrameworkElement CreateExit(double w, double h)
    {
        var c = new Canvas { Width = w, Height = h };
        Put(c, new Border
        {
            Width = w, Height = h - 16,
            Background = B("#4E342E"),
            BorderBrush = B("#FFD54F"), BorderThickness = new Thickness(4),
            CornerRadius = new CornerRadius(6, 6, 0, 0),
        }, 0, 16);
        Put(c, new Ellipse { Width = 6, Height = 6, Fill = B("#FFD54F") }, w - 14, h / 2 + 8);
        Put(c, new Border
        {
            Width = w, Height = 15, Background = B("#2E7D32"), CornerRadius = new CornerRadius(3),
            Child = new TextBlock
            {
                Text = "EXIT", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            },
        }, 0, 0);
        return c;
    }

    public static Border CreateBubble(string text) => new()
    {
        Background = Brushes.White,
        BorderBrush = Dark,
        BorderThickness = new Thickness(2),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(7, 3, 7, 3),
        IsHitTestVisible = false,
        Child = new TextBlock { Text = text, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = Dark },
    };

    private static T Put<T>(Panel panel, T element, double x, double y) where T : UIElement
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
        panel.Children.Add(element);
        return element;
    }

    private static PointCollection Pts(string s) => PointCollection.Parse(s);

    private static SolidColorBrush B(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
