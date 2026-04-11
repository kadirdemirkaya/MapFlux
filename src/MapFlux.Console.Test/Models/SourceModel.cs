namespace MapFlux.Console.Test.Models;

public class SourceModel
{
    public string Name { get; set; }
    public int Age { get; set; }
    public AddressModel Address { get; set; }
    public List<HobbyModel> Hobbies { get; set; }
}

public class AddressModel
{
    public string Street { get; set; }
    public string City { get; set; }

    [PropertyMapping("Location")]
    public double LocationCode { get; set; }
}

public class HobbyModel
{
    public string Name { get; set; }
    public int Years { get; set; }

    [PropertyMapping("CreatedDateOnUTC")]
    public DateTime CreatedAt { get; set; }
}
