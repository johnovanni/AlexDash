namespace AlexDash.Game;

/// <summary>Every gameplay number in one place.</summary>
public static class Tuning
{
    public const double FieldW = 1000, FieldH = 600;
    public const double AlexW = 30, AlexH = 46;
    public const double CrateSize = 34;

    public const double PlayerSpeed = 230, SlowedSpeed = 85;
    public const double SlowDuration = 3.0, HitGrace = 0.75;
    public const double BumpSlowDuration = 1.5, BumpSlowFactor = 0.4;
    public const double KnockbackSpeed = 300, KnockbackDecay = 10; // ≈ 30px shove
    public const double PairChatterCooldown = 2.5;

    public const double BoxStuckTime = 1.0, BoxClimbOutGrace = 1.0;

    public const double ControllerW = 18, ControllerH = 11;
    public const double ControllerSpeed = 420, ControllerRange = 450, ControllerLandedTime = 1.0;
    public const double ControllerKnockback = 360, ControllerHitSlow = 1.0;
    public const double RageMin = 4, RageMax = 9, ControllerReArm = 2.0;
    public const double RageWarning = 0.6; // gamers visibly shake this long before throwing

    public const int StartingLives = 3;
    public const double LevelEndPause = 3;

    public static double TimeLimit(int level) => Math.Max(15, 32 - 2 * (level - 1));
    public static int TimeBonus(int secondsLeft) => secondsLeft * 100;
    public static int LevelBonus(int level) => level * 250;
}
