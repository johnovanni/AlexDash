namespace AlexDash.Game.Tests;

public class LevelBuilderTests
{
    public static TheoryData<int> Seeds => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Start_and_exit_are_kept_clear(int seed)
    {
        for (int level = 1; level <= 10; level++)
        {
            var world = LevelBuilder.Build(level, new Random(seed));

            Assert.All(world.Obstacles, o => Assert.False(o.Bounds.Intersects(world.ExitZone)));
            Assert.All(world.Obstacles, o => Assert.False(o.Bounds.Intersects(world.StartZone)));
            Assert.All(world.Npcs, n => Assert.False(n.Bounds.Intersects(world.StartZone)));
            Assert.True(Physics.InField(world.Player.Bounds));
            Assert.True(Physics.InField(world.Exit));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Every_pusher_has_a_crate_and_ids_are_unique(int seed)
    {
        var world = LevelBuilder.Build(5, new Random(seed));

        Assert.All(world.Npcs.Where(n => n.Kind == AlexKind.Pusher), p =>
        {
            Assert.NotNull(p.Crate);
            Assert.Contains(p.Crate, world.Obstacles);
        });
        var ids = world.Alexes.Select(a => a.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Later_levels_are_busier()
    {
        var first = LevelBuilder.Build(1, new Random(1));
        var fifth = LevelBuilder.Build(5, new Random(1));

        Assert.True(fifth.Npcs.Count > first.Npcs.Count);
        Assert.True(fifth.Obstacles.Count > first.Obstacles.Count);
    }
}
