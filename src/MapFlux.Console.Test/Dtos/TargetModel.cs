namespace MapFlux.Console.Test.Dtos;

public class TargetModel
{
    public string Name { get; set; }
    public int Age { get; set; }
    public TargetAddressModel Address { get; set; }
    public List<TargetHobbyModel> Hobbies { get; set; }
}

public class TargetAddressModel
{
    public string Street { get; set; }
    public string City { get; set; }

    [PropertyMapping("Location")]
    public double LocationID { get; set; }
}

public class TargetHobbyModel
{
    public string Name { get; set; }
    public int Years { get; set; }

    [PropertyMapping("CreatedDateOnUTC")]
    public DateTime CreatedDate { get; set; }
}
