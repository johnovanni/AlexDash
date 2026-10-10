namespace AlexDash.Game.Tests;

public class PhysicsTests
{
    [Fact]
    public void Overlap_pushes_out_along_the_shallowest_axis()
    {
        var a = new Box(0, 0, 10, 10);
        var b = new Box(8, 2, 10, 10); // overlaps 2 in x, 8 in y

        Assert.True(Physics.Overlap(a, b, out double dx, out double dy));
        Assert.Equal(-2, dx);
        Assert.Equal(0, dy);
    }

    [Fact]
    public void Touching_boxes_do_not_overlap()
    {
        Assert.False(Physics.Overlap(new Box(0, 0, 10, 10), new Box(10, 0, 10, 10), out _, out _));
    }

    [Fact]
    public void Walls_bounce_an_Alex_back_into_the_field()
    {
        var a = new Alex { W = 30, H = 46, X = -5, Y = Tuning.FieldH, Vx = -100, Vy = 50 };

        Physics.BounceOffWalls(a);

        Assert.Equal(0, a.X);
        Assert.Equal(Tuning.FieldH - 46, a.Y);
        Assert.Equal(100, a.Vx);
        Assert.Equal(-50, a.Vy);
    }

    [Fact]
    public void Shift_moves_a_pusher_with_its_crate()
    {
        var crate = new Obstacle { X = 40, Y = 0, W = 34, H = 34 };
        var pusher = new Alex { X = 0, Y = 0, W = 30, H = 46, Crate = crate };

        Physics.Shift(pusher, 5, -3);

        Assert.Equal((5.0, -3.0), (pusher.X, pusher.Y));
        Assert.Equal((45.0, -3.0), (crate.X, crate.Y));
    }
}
