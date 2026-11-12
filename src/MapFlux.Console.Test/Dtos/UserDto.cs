namespace MapFlux.Console.Test.Dtos;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string EmailAddress { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public List<string> Titles { get; set; } = null!;
    public AddressDto AddressDto { get; set; } = null!;
    public List<UserDetailDto> UserDetailDtos { get; set; } = null!;
}

public class AddressDto
{
    public string StreetName { get; set; } = null!;
    public string CityName { get; set; } = null!;
}

public class UserDetailDto
{
    public string TelNo { get; set; } = null!;
    public int Age { get; set; }
}
