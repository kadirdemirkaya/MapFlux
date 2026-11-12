namespace MapFlux.Unit.Test.Models;

public class ListSource
{
    public List<ItemSource> Items { get; set; } = null!;
}

public class ItemSource
{
    public string Key { get; set; } = null!;
}
