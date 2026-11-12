namespace MapFlux.Unit.Test.Dtos;

public class DeepTarget
{
    public Level1Target Level1 { get; set; } = null!;
}

public class Level1Target
{
    public string Name { get; set; } = null!;
    public Level2Target Level2 { get; set; } = null!;
}

public class Level2Target
{
    public string Name { get; set; } = null!;
    public Level3Target Level3 { get; set; } = null!;
}

public class Level3Target
{
    public string Name { get; set; } = null!;
}
