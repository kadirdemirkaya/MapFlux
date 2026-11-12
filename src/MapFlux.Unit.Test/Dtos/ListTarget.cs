namespace MapFlux.Unit.Test.Dtos;

public class ListTarget
{
    public List<ItemTarget> Items { get; set; } = null!;
}

public class ItemTarget
{
    public string Key { get; set; } = null!;
}
