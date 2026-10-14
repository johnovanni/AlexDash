namespace AlexDash.Game;

public abstract class Entity
{
    public double X, Y, W, H;

    public Box Bounds => new(X, Y, W, H);
    public double CenterX => X + W / 2;
    public double CenterY => Y + H / 2;
}

public enum ObstacleKind { Cone, TrashCan, Bush, Rock, Crate }

public sealed class Obstacle : Entity
{
    public ObstacleKind Kind;
}

public enum AlexKind { Player, Skater, Runner, Pusher, Gamer }

public sealed class Alex : Entity
{
    public int Id;
    public AlexKind Kind;
    public double Vx, Vy, Speed;
    public double TurnTimer;        // NPCs: time until they pick a new direction
    public double SlowTimer;        // > 0 means slowed (obstacle hit, bump, or hit by a controller)
    public double HitImmunity;      // player only: > 0 means obstacle hits don't re-trigger
    public double KnockVx, KnockVy; // knockback velocity that fades out
    public Obstacle? Crate;         // pushers only

    // Falling headfirst into a crate.
    public Obstacle? InBox;               // the crate they're stuck in, if any
    public double BoxTimer;               // time until they climb out
    public double BoxImmunity;            // > 0 just climbed out: crates are solid for a moment
    public double BoxEntryX, BoxEntryY;   // direction they fell in from, to climb out the same side

    // Gamers.
    public bool HasController;
    public double RageTimer;   // time until they yell "Hackers!" and throw it
    public double ReArmTimer;  // time until they have a new controller
    public int FaceX = 1;      // which way they're facing when standing still

    public bool IsPlayer => Kind == AlexKind.Player;
}

/// <summary>A game controller thrown by a gamer Alex.</summary>
public sealed class Projectile : Entity
{
    public required Alex Thrower;
    public double Vx, Vy, Traveled;
    public bool Landed;
    public double LandedTimer; // after landing it lies on the ground this long, then disappears
}
