namespace AlexDash.Game;

/// <summary>An axis-aligned rectangle.</summary>
public readonly record struct Box(double X, double Y, double W, double H)
{
    public double Right => X + W;
    public double Bottom => Y + H;

    /// <summary>True if the boxes overlap or touch.</summary>
    public bool Intersects(Box other) =>
        other.X <= Right && other.Right >= X && other.Y <= Bottom && other.Bottom >= Y;

    public Box Inflate(double by) => new(X - by, Y - by, W + 2 * by, H + 2 * by);
}
