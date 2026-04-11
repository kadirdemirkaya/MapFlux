using MapFlux;
using MapFlux.Console.Test.Models;
using MapFlux.Console.Test.Dtos;
using MapFlux.Console.Test.Profiles;

Console.WriteLine("========================================");
Console.WriteLine("MapFlux Console Test - All Features Demo");
Console.WriteLine("========================================\n");

// ============================================================
// 1. Profile-based Mapping (ForMember, Nested, Collections)
// ============================================================
Console.WriteLine("1. Profile-based Mapping Demo");
Console.WriteLine("------------------------------");

var mapper = new Mapper();
mapper.CreateMap<UserProfile>();

User user = new User
{
    Id = 1,
    Name = "John Doe",
    Email = "john.doe@example.com",
    PhoneNumber = "00000999988",
    Titles = new() { "Developer", "Architect", "Manager" },
    Address = new Address { Street = "Main St", City = "Springfield" },
    UserDetails = new()
    {
        new(){Age = 25,TelNo = "555-0101"},
        new(){Age = 30,TelNo = "555-0102"}
    }
};

UserDto userDto = mapper.Map<User, UserDto>(user);

Console.WriteLine($"UserDto: {userDto.UserId}, {userDto.FullName}, {userDto.EmailAddress}");
Console.WriteLine($"AddressDto: {userDto.AddressDto.StreetName}, {userDto.AddressDto.CityName}");
Console.WriteLine($"Titles: {string.Join(", ", userDto.Titles)}");
Console.WriteLine("UserDetails:");
foreach (var uds in userDto.UserDetailDtos)
{
    Console.WriteLine($"  - Age: {uds.Age}, TelNo: {uds.TelNo}");
}

// ============================================================
// 2. Configuration Validation Demo
// ============================================================
Console.WriteLine("\n2. Configuration Validation Demo");
Console.WriteLine("---------------------------------");

try
{
    mapper.AssertConfigurationIsValid();
    Console.WriteLine("Configuration is valid!");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}

// ============================================================
// 3. ReverseMap Demo
// ============================================================
Console.WriteLine("\n3. ReverseMap Demo");
Console.WriteLine("------------------");

var reverseMapper = new Mapper();
reverseMapper.CreateMap<ReverseUserProfile>();

var dto = new ReverseUserDto { Id = 100, FullName = "Jane Smith" };
var entity = reverseMapper.Map<ReverseUserDto, ReverseUser>(dto);

Console.WriteLine($"DTO -> Entity: UserId={entity.UserId}, Name={entity.Name}");

// ============================================================
// 4. Ignore() Demo
// ============================================================
Console.WriteLine("\n4. Ignore() Demo");
Console.WriteLine("------------------");

var ignoreMapper = new Mapper();
ignoreMapper.CreateMap<IgnoreProfile>();

var sourceWithSecret = new PublicData { Id = 1, Name = "Public", SecretKey = "SHHH-12345" };
var publicDto = ignoreMapper.Map<PublicData, PublicDataDto>(sourceWithSecret);

Console.WriteLine($"Source SecretKey: {sourceWithSecret.SecretKey}");
Console.WriteLine($"Dto SecretKey: {(string.IsNullOrEmpty(publicDto.SecretKey) ? "[IGNORED - null]" : publicDto.SecretKey)}");

// ============================================================
// 5. NullSubstitute() Demo
// ============================================================
Console.WriteLine("\n5. NullSubstitute() Demo");
Console.WriteLine("-------------------------");

var nullSubMapper = new Mapper();
nullSubMapper.CreateMap<NullSubstituteProfile>();

var productWithNull = new Product { Id = 1, Name = null };
var productDto = nullSubMapper.Map<Product, ProductDto>(productWithNull);

Console.WriteLine($"Source Name: {(productWithNull.Name == null ? "[null]" : productWithNull.Name)}");
Console.WriteLine($"Dto Name: {productDto.Name} [default value substituted]");

// ============================================================
// 6. Convention-based Mapping (no ForMember needed)
// ============================================================
Console.WriteLine("\n6. Convention-based Mapping Demo");
Console.WriteLine("----------------------------------");

var conventionMapper = new Mapper();
conventionMapper.CreateMap<ConventionProfile>();

var customer = new Customer { Id = 42, FirstName = "Alice", LastName = "Wonderland", Email = "alice@example.com" };
var customerDto = conventionMapper.Map<Customer, CustomerDto>(customer);

Console.WriteLine($"CustomerDto: Id={customerDto.Id}, FirstName={customerDto.FirstName}, LastName={customerDto.LastName}, Email={customerDto.Email}");
Console.WriteLine("(Properties matched automatically by name!)");

// ============================================================
// 7. ModelMapper (Attribute-based) Demo
// ============================================================
Console.WriteLine("\n7. ModelMapper (Attribute-based) Demo");
Console.WriteLine("--------------------------------------");

var source = new SourceModel
{
    Name = "John",
    Age = 30,
    Address = new AddressModel
    {
        Street = "123 Main St",
        City = "New York",
        LocationCode = 123.456
    },
    Hobbies = new List<HobbyModel>
    {
        new HobbyModel { Name = "Reading", Years = 5, CreatedAt = DateTime.UtcNow },
        new HobbyModel { Name = "Swimming", Years = 3, CreatedAt = DateTime.UtcNow.AddSeconds(10) },
        new HobbyModel { Name = "Cycling", Years = 2, CreatedAt = DateTime.UtcNow.AddSeconds(20) }
    }
};

var target = ModelMapper.Map<SourceModel, TargetModel>(source);

Console.WriteLine($"Name: {target.Name}, Age: {target.Age}");
Console.WriteLine($"Address: {target.Address.Street}, {target.Address.City}, LocationID: {target.Address.LocationID}");
foreach (var hobby in target.Hobbies)
{
    Console.WriteLine($"Hobby: {hobby.Name}, Years: {hobby.Years}, CreatedAt: {hobby.CreatedDate}");
}

Console.WriteLine("\n========================================");
Console.WriteLine("All demos completed successfully!");
Console.WriteLine("========================================");