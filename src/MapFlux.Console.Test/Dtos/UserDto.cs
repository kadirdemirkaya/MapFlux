namespace MapFlux.Console.Test.Dtos;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public string Phone { get; set; }
    public List<string> Titles { get; set; }
    public AddressDto AddressDto { get; set; }
    public List<UserDetailDto> UserDetailDtos { get; set; }
}

public class AddressDto
{
    public string StreetName { get; set; }
    public string CityName { get; set; }
}

public class UserDetailDto
{
    public string TelNo { get; set; }
    public int Age { get; set; }
}
