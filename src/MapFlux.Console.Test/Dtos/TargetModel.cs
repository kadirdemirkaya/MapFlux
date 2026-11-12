namespace MapFlux.Console.Test.Dtos;

public class TargetModel
{
    public string Name { get; set; } = null!;
    public int Age { get; set; }
    public TargetAddressModel Address { get; set; } = null!;
    public List<TargetHobbyModel> Hobbies { get; set; } = null!;
}

public class TargetAddressModel
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;

    [PropertyMapping("Location")]
    public double LocationID { get; set; }
}

public class TargetHobbyModel
{
    public string Name { get; set; } = null!;
    public int Years { get; set; }

    [PropertyMapping("CreatedDateOnUTC")]
    public DateTime CreatedDate { get; set; }
}
