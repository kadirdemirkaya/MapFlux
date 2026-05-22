# MapFlux

| Package | Downloads | License |
|---------|-----------|---------|
| [![NuGet](https://img.shields.io/nuget/v/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![Downloads](https://img.shields.io/nuget/dt/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/kadirdemirkaya/MapFlux/blob/main/LICENSE.txt) |

MapFlux is a .NET object-to-object mapping library that gives you **two complete mappers in one package** -- a Profile-based mapper with expression-compiled mappings and a static attribute-driven mapper for convention-based scenarios. Pick the style that fits your project.

---

## How It Works

MapFlux contains two independent mapping engines:

**Profile-based Mapper** -- When you call `CreateMap<TProfile>()`, each mapping configuration is analyzed at setup time. The engine uses expression trees to build a mapping plan and compiles it into a cached delegate. Properties are matched by convention (case-insensitive name lookup) unless overridden with `ForMember`. At runtime, the compiled delegate executes directly, eliminating per-call reflection overhead. Nested objects and collections are resolved by looking up registered mappings from the same cache.

**ModelMapper** -- A static entry point that requires no configuration. It reflects on source and target types, matches properties by name or `[PropertyMapping]` attribute, and recursively maps complex types and collections via reflection. Ideal for quick transformations where you want to avoid setting up profiles.

---

## Features

- **Dual Mapping Architecture** -- Two independent mapping subsystems in a single library. Profile-based for explicit control, ModelMapper for convention-based instant mapping.
- **Expression-Compiled Mappings** -- Profile-based mapper builds expression trees at configuration time and compiles them into cached delegates, removing reflection from the hot path.
- **Attribute-based Mapping** -- Use `[PropertyMapping]` on properties to override names. ModelMapper picks them up automatically with no configuration.
- **Fluent Member Configuration** -- Clean API for custom member mapping, ignoring properties, and null substitution.
- **No External Dependencies** -- Pure .NET with zero third-party dependencies.

---

## Installation

```bash
dotnet add package MapFlux
```

```xml
<PackageReference Include="MapFlux" Version="1.1.0" />
```

### Supported Frameworks

- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

---

## Usage

### Approach 1: Profile-based Mapping (Expression-Compiled)

Define mappings in a Profile class with full control over member configuration:

```csharp
public class UserProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<User, UserDto>(m =>
        {
            m.ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name));
            m.ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(src => src.Email));
        });
    }
}

var mapper = new Mapper();
mapper.CreateMap<UserProfile>();

var user = new User { Name = "John Doe", Email = "john@example.com" };
var userDto = mapper.Map<User, UserDto>(user);
```

### Approach 2: Attribute-based Mapping (ModelMapper)

For quick, convention-driven mapping with optional attribute overrides:

```csharp
public class SourceModel
{
    public string Name { get; set; }

    [PropertyMapping("Location")]
    public double LocationCode { get; set; }
}

public class TargetModel
{
    public string Name { get; set; }

    [PropertyMapping("Location")]
    public double LocationID { get; set; }
}

var target = ModelMapper.Map<SourceModel, TargetModel>(source);
```

---

## Advanced Features

### Reverse Mapping

Profile-based mapper supports explicit two-way mapping:

```csharp
config.CreateMap<User, UserDto>(m =>
{
    m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.UserId));
    m.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Name));
});

config.CreateMap<UserDto, User>(m =>
{
    m.ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Id));
    m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName));
});
```

### Ignoring Properties

Skip specific destination properties during mapping:

```csharp
config.CreateMap<Source, Destination>(m =>
{
    m.ForMember(d => d.SecretKey, opt => opt.Ignore());
});
```

### Null Substitution

Provide fallback values when source properties are null:

```csharp
config.CreateMap<Product, ProductDto>(m =>
{
    m.ForMember(d => d.Name, opt =>
    {
        opt.MapFrom(s => s.Name);
        opt.NullSubstitute("N/A");
    });
});
```

### Configuration Validation

Validate all mappings at startup to catch configuration errors early:

```csharp
var mapper = new Mapper();
mapper.CreateMap<MyProfile>();
mapper.AssertConfigurationIsValid(); // Throws if any properties are unmapped
```

### Convention-based Mapping

Both mappers automatically match properties by name (case-insensitive). Profile-based mapper requires no explicit `ForMember` calls for matching property names:

```csharp
public class SimpleProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        config.CreateMap<Source, Destination>(m => { });
    }
}
```

---

## API Reference

### Mapper

| Method | Description |
|--------|-------------|
| `CreateMap<TProfile>()` | Registers a mapping profile (expression-compiled) |
| `Map<TSource, TDestination>()` | Maps an object to the destination type |
| `AssertConfigurationIsValid()` | Validates all registered mappings |

### IMappingExpression<TSource, TDestination>

| Method | Description |
|--------|-------------|
| `ForMember<TMember>()` | Configures mapping for a specific destination member |
| `ReverseMap()` | Registers a convention-based reverse mapping |

### IMemberConfigurationExpression

| Method | Description |
|--------|-------------|
| `MapFrom()` | Specifies the source member |
| `Ignore()` | Excludes the destination member from mapping |
| `NullSubstitute()` | Provides a default value when source is null |

### ModelMapper (Static)

| Method | Description |
|--------|-------------|
| `Map<TSource, TTarget>()` | Convention + attribute-based automatic mapping |

## Project Structure

- `MapFlux` -- Core library with both mapping engines
- `MapFlux.Console.Test` -- Demo and usage examples
- `MapFlux.Unit.Test` -- Comprehensive xUnit test suite

Run tests:

```bash
cd src/MapFlux.Unit.Test
dotnet test
```

---

## License

MIT
