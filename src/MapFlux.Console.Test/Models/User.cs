namespace MapFlux.Console.Test.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public List<string> Titles { get; set; }
    public Address Address { get; set; }
    public List<UserDetail> UserDetails { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

public class UserDetail
{
    public string TelNo { get; set; }
    public int Age { get; set; }
}
