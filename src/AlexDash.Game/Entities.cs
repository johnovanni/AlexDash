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

public enum AlexKind { Player, Skater, Runner, Pusher }

public sealed class Alex : Entity
{
    public int Id;
    public AlexKind Kind;
    public double Vx, Vy, Speed;
    public double TurnTimer;        // NPCs: time until they pick a new direction
    public double SlowTimer;        // > 0 means slowed (obstacle hit or bumped by another Alex)
    public double HitImmunity;      // player only: > 0 means obstacle hits don't re-trigger
    public double KnockVx, KnockVy; // player only: knockback velocity that fades out
    public Obstacle? Crate;         // pushers only

    public bool IsPlayer => Kind == AlexKind.Player;
}
