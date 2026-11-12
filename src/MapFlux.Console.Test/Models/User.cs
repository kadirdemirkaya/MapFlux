namespace MapFlux.Console.Test.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public List<string> Titles { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public List<UserDetail> UserDetails { get; set; } = null!;
}

public class Address
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
}

public class UserDetail
{
    public string TelNo { get; set; } = null!;
    public int Age { get; set; }
}
