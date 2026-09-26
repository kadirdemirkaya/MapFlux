# MapFlux

<p align="center"><img src="https://raw.githubusercontent.com/kadirdemirkaya/MapFlux/main/assets/icon.png" alt="MapFlux logo" width="112" /></p>

| Package | Downloads | License |
|---------|-----------|---------|
| [![NuGet](https://img.shields.io/nuget/v/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![Downloads](https://img.shields.io/nuget/dt/MapFlux)](https://www.nuget.org/packages/MapFlux) | [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/kadirdemirkaya/MapFlux/blob/main/LICENSE.txt) |

MapFlux is a .NET object-to-object mapping library that gives you **two complete mappers in one package** -- a Profile-based mapper with expression-compiled mappings and a static attribute-driven mapper for convention-based scenarios. Pick the style that fits your project.

---

## How It Works

MapFlux contains two independent mapping engines:

**Profile-based Mapper** -- When you call `CreateMap<TProfile>()`, each mapping configuration is analyzed at setup time. The engine uses expression trees to build one typed mapping plan per source/destination pair and compiles it into a cached delegate. Properties are matched by convention (case-insensitive name lookup) unless overridden with `ForMember`. At runtime the compiled delegate executes directly, eliminating per-call reflection overhead: a member that needs no nested or element map is read, converted and assigned by typed code, without boxing its value. For a member that does need one, the nested or element map is looked up the first time that member is mapped and reused afterwards, so a map registered later than the map using it still takes effect.

**ModelMapper** -- A static entry point that requires no configuration. It reflects on source and target types, matches properties by name or `[PropertyMapping]` attribute, and recursively maps complex types and collections. The member plan for a source/target type pair -- the attributes, the matched target property and the compiled property accessors -- is built on first use and cached, and a nested member or collection element is mapped through a cached delegate, so repeated mappings of the same pair do not pay for that reflection again. Ideal for quick transformations where you want to avoid setting up profiles.

---

## Features

- **Dual Mapping Architecture** -- Two independent mapping subsystems in a single library. Profile-based for explicit control, ModelMapper for convention-based instant mapping.
- **Expression-Compiled Mappings** -- Profile-based mapper builds expression trees at configuration time and compiles them into one cached typed delegate per mapping, removing reflection and member-value boxing from the hot path.
- **Attribute-based Mapping** -- Use `[PropertyMapping]` on properties to override names. ModelMapper picks them up automatically with no configuration.
- **Fluent Member Configuration** -- Clean API for custom member mapping, ignoring properties, and null substitution.
- **Type Conversion** -- Numeric, `Nullable<T>` and enum member types are converted inside the compiled plan; a pair with no conversion reports the member and both type names.
- **Collection Shapes** -- Arrays, `List<T>` and the collection interfaces map into one another, as a member and at the top level, with the element map applied to every element. ModelMapper accepts the same member shapes plus `Dictionary<,>`, with no configuration.
- **Mapping onto an Existing Object** -- `Map(source, destination)` updates an instance you already own instead of creating one; ignored members, members without a source counterpart and members whose source value is `null` keep the value they hold.
- **Constructor-Based Destinations** -- A destination without a parameterless constructor, such as a positional `record`, is built through the constructor whose parameter names match its members; `ForMember` and the type conversions apply to constructor arguments too.
- **Opt-in Strict Validation** -- `AssertConfigurationIsValid(true)` checks nested and element maps and type compatibility for every registered mapping, on top of the default unmapped-property check.
- **Cycle-Safe by Default** -- A cyclic or excessively deep source graph raises an `InvalidOperationException` instead of overflowing the stack; the limit is configurable with `MaxDepth`.
- **No External Dependencies** -- Pure .NET with zero third-party dependencies.
- **Documented and Source-Linked** -- Every public API member ships XML documentation for IntelliSense, and the package is source-linked so debugging into MapFlux fetches the matching source from GitHub.

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
- .NET 10.0

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

Registering profiles one by one does not scale once an assembly holds many of them. Register every
concrete, parameterless-constructor `Profile` found in one or more assemblies with a single call;
registering the same profile again afterwards does not throw:

```csharp
mapper.CreateMapsFromAssemblies(typeof(UserProfile).Assembly);
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

`MapFrom` is optional: `opt.NullSubstitute(...)` alone falls back to the destination member's
name-matched source property. If no such property exists, `CreateMap` throws describing which
member and destination type need an explicit `MapFrom`.

### Configuration Validation

Validate all mappings at startup to catch configuration errors early:

```csharp
var mapper = new Mapper();
mapper.CreateMap<MyProfile>();
mapper.AssertConfigurationIsValid(); // Throws if any properties are unmapped
```

A member whose type needs a nested or element map that was never registered passes this check --
`AssertConfigurationIsValid()` only looks at property names. `Map` still reports it, with an
`InvalidOperationException` naming the destination member and both types, the moment that member is
mapped:

```csharp
public class Order { public Customer Customer { get; set; } }
public class OrderDto { public CustomerDto Customer { get; set; } }

config.CreateMap<Order, OrderDto>(m => { }); // no CreateMap<Customer, CustomerDto>

mapper.AssertConfigurationIsValid();               // passes -- "Customer" matches by name
mapper.Map<Order, OrderDto>(order);                // throws: OrderDto.Customer needs Customer -> CustomerDto
```

Pass `true` to check every registered mapping's nested and element maps and type compatibility
(including the [Type Conversion](#type-conversion) rules) up front instead, and get every problem in
one exception:

```csharp
mapper.AssertConfigurationIsValid(true); // throws: OrderDto.Customer needs Customer -> CustomerDto
```

This is opt-in and additive: `AssertConfigurationIsValid()` keeps validating only property names, so a
configuration that relies on today's default behavior keeps starting up unchanged.

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

### Type Conversion

When a source and a destination member have different types, the profile-based mapper converts the value inside the compiled plan -- by name matching and through `MapFrom` alike:

- numeric conversions in both directions: `int` -> `long`, `long` -> `int`, `int` -> `decimal`, `float` -> `double`
- `Nullable<T>` in both directions: `int` -> `long?`, `int?` -> `long`. A `null` source member leaves the destination member at the default value of its type
- enums and their underlying numeric type: `Status` -> `int`, `int` -> `Status`

```csharp
public class Order { public int Quantity { get; set; } }
public class OrderDto { public long Quantity { get; set; } }

var dto = mapper.Map<Order, OrderDto>(new Order { Quantity = 5 }); // dto.Quantity == 5L
```

A pair with no such conversion -- `string` -> `int`, for example -- throws an `InvalidOperationException` naming the member and both type names when `Map` is called. Configuration and `AssertConfigurationIsValid()` stay silent about it, so registering a map never fails at startup.

### Collection Shapes

Any source `IEnumerable<T>` except `string` -- an array, a `List<T>`, a collection interface, a LINQ result -- maps to any of these destination shapes, both as a member and in a top-level `Map` call:

`T[]` -- `List<T>` -- `IEnumerable<T>` -- `ICollection<T>` -- `IList<T>` -- `IReadOnlyCollection<T>` -- `IReadOnlyList<T>`

```csharp
public class Order { public Item[] Items { get; set; } }
public class OrderDto { public IReadOnlyList<ItemDto> Items { get; set; } }

config.CreateMap<Item, ItemDto>(m => { });
config.CreateMap<Order, OrderDto>(m => { });

var dto = mapper.Map<Order, OrderDto>(order);                  // Item[] -> IReadOnlyList<ItemDto>
var dtos = mapper.Map<Item[], List<ItemDto>>(order.Items);     // top-level, same rules
```

The element is resolved in this order:

- a registered element map is applied to every element
- otherwise, when the destination type already accepts the source collection itself, the source instance is passed through
- otherwise, when the source element type is assignable to the destination element type, the elements are copied into the destination shape
- otherwise `Map` throws an `InvalidOperationException` naming the member and both element types

A `null` collection leaves the destination member `null` and returns `null` from a top-level `Map`; a `null` element stays `null` in the result. A `string` is never treated as a collection of characters.

### Mapping onto an Existing Object

`Map(source, destination)` maps onto an instance you already have -- an entity loaded from a database,
a view model bound to a form -- instead of building a new one. It returns that same instance:

```csharp
var entity = repository.Get(id);                 // tracked, has values you must not lose

mapper.Map(updateRequest, entity);               // same instance, updated in place
```

Only members the mapping actually writes are touched. Everything else the destination already holds
survives the call:

| Destination member | Result |
|--------------------|--------|
| Mapped from a non-`null` source value | Overwritten with the mapped value |
| `Ignore()`d | Kept as it is |
| No matching source member | Kept as it is |
| Source value is `null` (and no `NullSubstitute`) | Kept as it is |
| A nested object or collection that is mapped | Replaced by a newly built instance, not merged |

`source` being `null` returns the destination untouched; a `null` destination throws an
`ArgumentNullException`, because there is nothing to map onto. The overload maps onto a single object:
for a collection, use `Map<TSource, TDestination>(source)` to build a new one.

The same overload is available through the `IExistingDestinationMapper` interface, so it can be
injected without changing `IMapper`:

```csharp
services.AddSingleton<Mapper>();
services.AddSingleton<IMapper>(sp => sp.GetRequiredService<Mapper>());
services.AddSingleton<IExistingDestinationMapper>(sp => sp.GetRequiredService<Mapper>());
```

### Constructor-Based Destinations

A destination type does not need a parameterless constructor. When it has none, MapFlux looks for a
single public constructor whose parameter names all match members of the destination type
(case-insensitive) and builds the instance through it -- which is exactly what a positional `record`
gives you:

```csharp
public record CustomerDto(int Id, string FullName)
{
    public string Country { get; set; }
}

config.CreateMap<Customer, CustomerDto>(m =>
    m.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName)));

var dto = mapper.Map<Customer, CustomerDto>(customer);
```

`ForMember` configures a constructor parameter through the member that parameter is bound to:
`MapFrom`, `NullSubstitute` and `Ignore` behave as they do for a settable member, and the numeric,
`Nullable<T>` and enum conversions apply to constructor arguments too. A parameter whose member is
`Ignore()`d, or whose member has no source counterpart, receives the default value of its type. A
nested object or collection argument is mapped with the registered map for that type pair, so a
record can hold other records. Members that are not bound to a constructor parameter -- `Country`
above -- are assigned after construction, as before.

Two destinations are rejected when `CreateMap` runs, with an `InvalidOperationException` naming the
destination type:

| Destination | Message |
|-------------|---------|
| No public constructor whose parameters all match members | Lists every public constructor with the parameters that have no matching member |
| More than one constructor whose parameters match members | Lists the matching constructors, so you can see which one to keep |

`ModelMapper` is unaffected: it still requires a parameterless constructor on both sides and reports
the member and the type when one is missing.

### Collection and Dictionary Members (ModelMapper)

`ModelMapper` maps a collection member without any configuration. The source member may be any
`IEnumerable<T>` except `string`, and the destination member may be an array, a `List<T>`, any other
`IList` implementation with a public parameterless constructor, or one of `IEnumerable<T>`,
`ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>` and `IReadOnlyList<T>`:

```csharp
public class Order
{
    public List<int> Quantities { get; set; }
    public string[] Tags { get; set; }
    public Item[] Items { get; set; }
    public Dictionary<string, Item> ItemsByCode { get; set; }
}

public class OrderDto
{
    public List<int> Quantities { get; set; }
    public string[] Tags { get; set; }
    public List<ItemDto> Items { get; set; }
    public Dictionary<string, ItemDto> ItemsByCode { get; set; }
}

var dto = ModelMapper.Map<Order, OrderDto>(order);
```

Each element is resolved on its own:

- a value type, a `Nullable<T>`, a `string` or any element already assignable to the destination element type is copied as it is
- a class element with a public parameterless constructor on both sides is mapped recursively, by name and `[PropertyMapping]`, like any nested member
- a nested collection element is mapped into the destination element shape
- otherwise `Map` throws an `InvalidOperationException` naming the destination member and both element types

A dictionary member maps into `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`,
`IReadOnlyDictionary<TKey, TValue>` or any other `IDictionary` implementation with a public
parameterless constructor. Keys and values follow the same element rules, so `Dictionary<string, int>`
is copied entry by entry while the values of `Dictionary<string, Item>` are mapped to `ItemDto`.

A `null` collection or dictionary member leaves the destination member `null`, and a `null` element
stays `null` in the result.

---

### Cyclic Graphs and Mapping Depth

Both mappers walk a source graph recursively, so an object that references itself -- directly, through
another object, or through a collection -- has no natural end. Instead of following such a graph until the
stack overflows and the process dies, both mappers count how deep the current `Map` call has gone and stop
at `MaxDepth`, which is `32` by default:

```csharp
var node = new Node { Id = 1 };
node.Next = node;

mapper.Map<Node, NodeDto>(node);              // InvalidOperationException
ModelMapper.Map<Node, NodeDto>(node);         // InvalidOperationException
```

The exception names both types and the limit that was hit, and it is an ordinary `InvalidOperationException`
you can catch. Mapping keeps working normally afterwards -- the depth counter is per call, so a failed map
leaves nothing behind.

A graph that is genuinely deeper than 32 levels is not a cycle, so the limit can be raised. It is per
`Mapper` instance, and a static property for `ModelMapper`:

```csharp
mapper.MaxDepth = 128;
ModelMapper.MaxDepth = 128;
```

Any value below `1` throws an `ArgumentOutOfRangeException`. When the cycle is not wanted in the destination
at all, `Ignore` on the member that closes it keeps the depth counter far from the limit:

```csharp
config.CreateMap<Node, NodeDto>(m => m.ForMember(d => d.Next, opt => opt.Ignore()));
```

---

## API Reference

### Mapper

| Method | Description |
|--------|-------------|
| `CreateMap<TProfile>()` | Registers a mapping profile (expression-compiled) |
| `CreateMapsFromAssemblies(params Assembly[] assemblies)` | Registers every concrete, parameterless-constructor `Profile` found in the given assemblies |
| `Map<TSource, TDestination>(TSource source)` | Maps an object to the destination type |
| `Map<TSource, TDestination>(TSource source, TDestination destination)` | Maps onto an existing destination instance and returns it; ignored, unmatched and `null`-sourced members keep their current value |
| `AssertConfigurationIsValid()` | Validates all registered mappings for unmapped destination properties |
| `AssertConfigurationIsValid(bool strict)` | `strict: true` additionally validates nested and element maps and type compatibility |
| `MaxDepth` | Maximum recursion depth of a single `Map` call (default `32`); a deeper or cyclic graph throws `InvalidOperationException` |

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
| `MaxDepth` | Maximum recursion depth of a single `Map` call (default `32`); a deeper or cyclic graph throws `InvalidOperationException` |

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
