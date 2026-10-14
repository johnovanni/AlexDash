namespace AlexDash.Game;

/// <summary>Everything on the field for one level.</summary>
public sealed class World(Alex player, Box exit, Box startZone)
{
    public Alex Player { get; } = player;
    public Box Exit { get; } = exit;

    /// <summary>Kept clear of obstacles and crates so the exit is always reachable.</summary>
    public Box ExitZone { get; } = exit.Inflate(70);

    /// <summary>Kept clear of obstacles and other Alexes so the player gets a fair start.</summary>
    public Box StartZone { get; } = startZone;

    public List<Obstacle> Obstacles { get; } = [];
    public List<Alex> Npcs { get; } = [];
    public List<Projectile> Projectiles { get; } = [];

    public IEnumerable<Alex> Alexes => Npcs.Prepend(Player);
}
