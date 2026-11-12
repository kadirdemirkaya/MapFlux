namespace MapFlux.Console.Test.Models;

public class SourceModel
{
    public string Name { get; set; } = null!;
    public int Age { get; set; }
    public AddressModel Address { get; set; } = null!;
    public List<HobbyModel> Hobbies { get; set; } = null!;
}

public class AddressModel
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;

    [PropertyMapping("Location")]
    public double LocationCode { get; set; }
}

public class HobbyModel
{
    public string Name { get; set; } = null!;
    public int Years { get; set; }

    [PropertyMapping("CreatedDateOnUTC")]
    public DateTime CreatedAt { get; set; }
}
