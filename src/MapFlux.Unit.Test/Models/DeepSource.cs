namespace MapFlux.Unit.Test.Models;

public class DeepSource
{
    public Level1Source Level1 { get; set; }
}

public class Level1Source
{
    public string Name { get; set; }
    public Level2Source Level2 { get; set; }
}

public class Level2Source
{
    public string Name { get; set; }
    public Level3Source Level3 { get; set; }
}

public class Level3Source
{
    public string Name { get; set; }
}
