using static AlexDash.Game.Tuning;

namespace AlexDash.Game;

public static class Physics
{
    /// <summary>If a and b overlap, returns the smallest (dx, dy) that moves a out of b.</summary>
    public static bool Overlap(Box a, Box b, out double dx, out double dy)
    {
        dx = dy = 0;
        double ox = Math.Min(a.Right, b.Right) - Math.Max(a.X, b.X);
        double oy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Y, b.Y);
        if (ox <= 0 || oy <= 0) return false;

        if (ox < oy) dx = a.X + a.W / 2 < b.X + b.W / 2 ? -ox : ox;
        else dy = a.Y + a.H / 2 < b.Y + b.H / 2 ? -oy : oy;
        return true;
    }

    public static bool InField(Box r) => r.X >= 0 && r.Y >= 0 && r.Right <= FieldW && r.Bottom <= FieldH;

    public static void ClampToField(Entity e)
    {
        e.X = Math.Clamp(e.X, 0, FieldW - e.W);
        e.Y = Math.Clamp(e.Y, 0, FieldH - e.H);
    }

    public static void BounceOffWalls(Alex a)
    {
        if (a.X < 0) { a.X = 0; a.Vx = Math.Abs(a.Vx); }
        if (a.X > FieldW - a.W) { a.X = FieldW - a.W; a.Vx = -Math.Abs(a.Vx); }
        if (a.Y < 0) { a.Y = 0; a.Vy = Math.Abs(a.Vy); }
        if (a.Y > FieldH - a.H) { a.Y = FieldH - a.H; a.Vy = -Math.Abs(a.Vy); }
    }

    /// <summary>Bounces velocity away along the axis of a separation vector.</summary>
    public static void Deflect(Alex a, double dx, double dy)
    {
        if (dx != 0) a.Vx = Math.Sign(dx) * Math.Abs(a.Vx);
        if (dy != 0) a.Vy = Math.Sign(dy) * Math.Abs(a.Vy);
    }

    /// <summary>Moves an Alex, and the crate it's pushing, if any.</summary>
    public static void Shift(Alex a, double dx, double dy)
    {
        a.X += dx; a.Y += dy;
        if (a.Crate is { } c) { c.X += dx; c.Y += dy; }
    }

    /// <summary>Shoves an Alex away from another one; the shove fades out over a fraction of a second.</summary>
    public static void KnockBack(Alex a, Alex away)
    {
        double dx = a.CenterX - away.CenterX, dy = a.CenterY - away.CenterY;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 0.001) { dx = -1; dy = 0; len = 1; }
        a.KnockVx = dx / len * KnockbackSpeed;
        a.KnockVy = dy / len * KnockbackSpeed;
    }
}
