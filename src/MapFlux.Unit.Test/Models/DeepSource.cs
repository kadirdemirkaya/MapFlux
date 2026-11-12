namespace MapFlux.Unit.Test.Models;

public class DeepSource
{
    public Level1Source Level1 { get; set; } = null!;
}

public class Level1Source
{
    public string Name { get; set; } = null!;
    public Level2Source Level2 { get; set; } = null!;
}

public class Level2Source
{
    public string Name { get; set; } = null!;
    public Level3Source Level3 { get; set; } = null!;
}

public class Level3Source
{
    public string Name { get; set; } = null!;
}
