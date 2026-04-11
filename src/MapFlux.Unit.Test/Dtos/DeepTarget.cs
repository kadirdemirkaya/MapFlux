namespace MapFlux.Unit.Test.Dtos;

public class DeepTarget
{
    public Level1Target Level1 { get; set; }
}

public class Level1Target
{
    public string Name { get; set; }
    public Level2Target Level2 { get; set; }
}

public class Level2Target
{
    public string Name { get; set; }
    public Level3Target Level3 { get; set; }
}

public class Level3Target
{
    public string Name { get; set; }
}
