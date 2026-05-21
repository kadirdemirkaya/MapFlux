# 🗺️ MapFlux

| Package | Downloads | License |
|---------|-----------|---------|
| [![NuGet](https://img.shields.io/nuget/v/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![Downloads](https://img.shields.io/nuget/dt/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/kadirdemirkaya/MapFlux/blob/main/LICENSE.txt) |

**MapFlux** is a lightweight, high-performance object-to-object mapping library for .NET designed for modern applications. It provides a flexible way to map complex objects using both **Profile-based** configurations and **Attribute-based** automatic mapping, with a focus on simplicity, performance, and developer productivity.

---

## 🚀 Features

- **⚡ High Performance:** Fast and efficient object mapping.
- **🛠️ Profile-based Configuration:** Define your mappings in separate profile classes for better organization.
- **🏷️ Attribute Mapping:** Use `[PropertyMapping]` attributes for quick and easy property name overrides.
- **🔄 Recursive Mapping:** Automatically handles nested objects and collections.
- **🎯 Fluent API:** Clean and expressive syntax for member-level mapping control.

---

## 📦 Installation

### NuGet Package Manager

```bash
dotnet add package MapFlux
```

### PackageReference

```xml
<PackageReference Include="MapFlux" Version="1.0.2" />
```

### Supported Frameworks

- .NET 6.0
- .NET 7.0
- .NET 8.0
- .NET 9.0

---

## 🛠️ Usage

### 1. Profile-based Mapping
Profiles allow you to define complex mapping logic, including type conversions and custom logic for specific members.

```csharp
// 1. Define your Profile
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

// 2. Initialize and Use Mapper
var mapper = new Mapper();
mapper.CreateMap<UserProfile>();

var user = new User { Name = "John Doe", Email = "john@example.com" };
var userDto = mapper.Map<User, UserDto>(user);
```

### 2. Attribute-based Mapping (ModelMapper)
For simpler scenarios where you want automatic mapping by property name (or custom naming via attributes), use the static `ModelMapper`.

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

// Automatic mapping with recursion support
var target = ModelMapper.Map<SourceModel, TargetModel>(source);
```

---

## 🎯 Advanced Features

### 3. Reverse Mapping (Two-way Mapping)
Define both forward and reverse mappings explicitly:

```csharp
public class UserProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        // Forward: User -> UserDto
        config.CreateMap<User, UserDto>(m =>
        {
            m.ForMember(d => d.Id, opt => opt.MapFrom(s => s.UserId));
            m.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Name));
        });
        
        // Reverse: UserDto -> User
        config.CreateMap<UserDto, User>(m =>
        {
            m.ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Id));
            m.ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName));
        });
    }
}

// Now you can map in both directions
var userDto = mapper.Map<User, UserDto>(user);
var user = mapper.Map<UserDto, User>(userDto);  // Reverse mapping
```

### 4. Ignoring Properties
Skip specific properties during mapping with `Ignore()`:

```csharp
config.CreateMap<Source, Destination>(m =>
{
    m.ForMember(d => d.SecretKey, opt => opt.Ignore());
});
```

### 5. Null Substitution
Provide default values when source properties are null:

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

### 6. Configuration Validation
Validate your mapping configuration at startup:

```csharp
var mapper = new Mapper();
mapper.CreateMap<MyProfile>();

// Throws InvalidOperationException if any properties are unmapped
mapper.AssertConfigurationIsValid();
```

### 7. Convention-based Mapping
Map properties automatically by matching names (case-insensitive):

```csharp
public class SimpleProfile : Profile
{
    public override void Configure(IMapperConfigurationExpression config)
    {
        // No explicit ForMember needed for matching property names
        config.CreateMap<Source, Destination>(m => { });
    }
}
```

---

## 📚 API Reference

### Mapper
| Method | Description |
|--------|-------------|
| `CreateMap<TProfile>()` | Registers a mapping profile |
| `Map<TSource, TDestination>()` | Maps an object to the destination type |
| `AssertConfigurationIsValid()` | Validates all registered mappings |

### IMappingExpression<TSource, TDestination>
| Method | Description |
|--------|-------------|
| `ForMember<TMember>()` | Configures mapping for a specific destination member |
| `CreateMap<TSource, TDestination>()` | Create separate reverse mappings for two-way mapping |

### IMemberConfigurationExpression
| Method | Description |
|--------|-------------|
| `MapFrom()` | Specifies the source property/expression |
| `Ignore()` | Skips mapping for this property |
| `NullSubstitute()` | Provides default value when source is null |

### ModelMapper (Static)
| Method | Description |
|--------|-------------|
| `Map<TSource, TTarget>()` | Attribute-based automatic mapping |

---

## 🧪 Unit Tests

MapFlux is fully tested with **xUnit**. The `MapFlux.Unit.Test` project covers:
- Basic property mapping.
- Custom member logic (`ForMember`).
- Reverse mapping (`ReverseMap`).
- Property ignoring (`Ignore`).
- Null substitution (`NullSubstitute`).
- Configuration validation (`AssertConfigurationIsValid`).
- Deeply nested objects (3+ levels).
- Collections mapping (`List<T>`).
- Attribute-based mapping overrides.
- Convention-based automatic mapping.

To run the tests:
```bash
cd src/MapFlux.Unit.Test
dotnet test
```

---

## 📁 Project Structure

- `MapFlux`: Core library containing the mapping engine.
- `MapFlux.Console.Test`: Demo project showcasing usage examples.
- `MapFlux.Unit.Test`: Comprehensive unit test suite.
