# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- `Mapper.Map<TSource, TDestination>(TSource source, TDestination destination)`: maps onto a destination instance the caller already owns and returns that same instance instead of creating a new one. Members that are `Ignore()`d, members the source has no counterpart for, and members whose source value is `null` without a `NullSubstitute` keep the value the destination already holds; a nested or collection member that is mapped is replaced by a newly built instance rather than merged into the existing one. A `null` source returns the destination untouched, a `null` destination throws `ArgumentNullException`, and a collection destination throws `InvalidOperationException` naming the shape. The overload is also reachable through the new `IExistingDestinationMapper` interface, which `Mapper` implements; `IMapper` is unchanged, so existing implementations of it keep compiling.
- `Mapper.MaxDepth` and `ModelMapper.MaxDepth`: the maximum recursion depth of a single `Map` call, `32` by default. Raise it for a graph that is genuinely deeper than the limit; a value below `1` throws `ArgumentOutOfRangeException`.
- `Mapper.AssertConfigurationIsValid(bool strict)` overload: `strict: true` additionally validates every registered mapping's nested and element maps and member type compatibility (including the numeric/`Nullable<T>`/enum conversions above), aggregating every problem into one `InvalidOperationException`. The existing `AssertConfigurationIsValid()` overload is unchanged and keeps validating only unmapped destination properties.
- `Mapper.Map` now maps any source `IEnumerable<T>` other than `string` — an array, a `List<T>`, a collection interface, a LINQ result — into `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>` and `IReadOnlyList<T>`, both for a member and for a top-level `Map` call. A registered element map is applied to every element; otherwise the source instance is passed through when the destination type already accepts it, or the elements are copied into the destination shape when the source element type is assignable. Previously only `List<T>` to `List<T>` worked, and an array, `IEnumerable<T>` or `IReadOnlyList<T>` destination failed. A `null` collection stays `null` and a `null` element is kept as `null`, as for a `List<T>`.

- `ModelMapper.Map` now maps a collection member whose elements are value types, `Nullable<T>` or `string` (`List<int>`, `string[]`), an array member in either direction (`Src[]` to `Dst[]`, `List<Src>` to `Dst[]`), a `Dictionary<,>` member and a nested collection element. The destination member may be an array, a `List<T>`, any other `IList` implementation with a public parameterless constructor, one of `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>` and `IReadOnlyList<T>`, or `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>` and any other `IDictionary` implementation. Dictionary keys and values follow the same element rules, so keys and value-type values are copied and class values are mapped recursively. Previously only `IEnumerable<Src>` to `List<Dst>` worked: a value-type element threw `ArgumentException`, an array destination `MissingMethodException` and a dictionary member `InvalidCastException`.

### Changed

- `Mapper.Map` is considerably faster and allocates far less. A mapping is now compiled into a single typed delegate per source/destination pair: a member that needs no nested or element map is read, converted and assigned by typed code instead of being boxed into `object` and written through a reflection-shaped setter, and the nested or element map of a member that needs one is resolved the first time that member is mapped instead of on every call. Measured over 200 000 mappings: an object graph with one `int` and one `string` member, one nested object and a two-element list took 2,06 µs and 576 bytes per mapping before and 0,38 µs and 264 bytes after; an object with ten scalar members took 0,23 µs and 232 bytes before and 0,08 µs and 88 bytes after — the same allocation as hand-written mapping code. Behaviour, results and error messages are unchanged, including a nested or element map registered after the map that uses it.
- The package description no longer calls the library "high-performance": it describes the compiled mapping plans instead, which is what the measurements above support.

- `ModelMapper.Map` is considerably faster and allocates far less. The member plan for a source/target type pair — the `[PropertyMapping]` attributes, the matched target property, and the accessors — is now built once per pair and reused, and a nested member or collection element is mapped through a cached delegate instead of a reflective call built on every mapping. Measured on the same object graph over 200 000 mappings: 5,50 µs and 1921 bytes per mapping before, 1,93 µs and 480 bytes after (0,40 µs once the code is fully warmed up). Behaviour, results and error messages are unchanged.

### Fixed

- `ModelMapper.Map` no longer throws `NullReferenceException` for a `null` element inside a collection member — the element stays `null` in the result, as it already did for `Mapper`. A collection member whose destination type is not a supported collection shape, or whose element can be neither mapped nor assigned, now throws an `InvalidOperationException` naming the destination member and both type names.
- A cyclic source graph no longer overflows the stack and kills the process. `Mapper.Map` and `ModelMapper.Map` now stop at `MaxDepth` and throw an `InvalidOperationException` naming both types and the limit, whether the cycle runs through a nested member or through a collection element. The depth is counted per `Map` call, so mapping keeps working after such a failure.
- `CreateMap` for a destination type without a public parameterless constructor now throws a descriptive `InvalidOperationException` naming the destination type, right when `CreateMap` is called. Previously it threw a `TypeInitializationException` and permanently poisoned that source/destination pair for the rest of the process, so even a later, correct registration for the same pair kept failing.
- A collection member or a top-level collection call whose elements can be neither mapped nor assigned now throws an `InvalidOperationException` naming the member and both element types, instead of reporting the two collection type names.

- `Mapper.Map` no longer throws `InvalidCastException` when a source and a destination member have different types. Numeric conversions in both directions, `Nullable<T>` in both directions, and enum to or from its underlying numeric type are now performed inside the compiled mapping plan — both for name-matched members and for `MapFrom`. A `null` source member still leaves the destination member at the default value of its type.
- A member pair with no available conversion, such as `string` to `int`, now throws an `InvalidOperationException` naming the member and both type names instead of a bare `InvalidCastException`. Configuration and `AssertConfigurationIsValid()` stay silent about such a pair, so registering a map never fails at startup.
- `Mapper.Map` no longer throws `NullReferenceException` for a `null` source, a `null` source list, or a `null` element inside a list (top-level or nested property). A `null` source now returns `default(TDestination)`, a `null` list returns `null`, and a `null` list element is kept as `null` in the mapped list.
- `ForMember` no longer throws `InvalidCastException` for a destination expression wrapped in a conversion, such as `d => (object)d.Name`.
- `ForMember` now throws a descriptive `ArgumentException` at configuration time for a destination expression that is not a direct member access on the destination parameter — including a nested path such as `d => d.Inner.Name`. Previously a nested path silently wrote the mapped value to the wrong top-level member instead of the intended nested one.
- An explicit `CreateMap<TDestination, TSource>` now always takes precedence over the convention-based map that `ReverseMap()` registers for the same type pair, regardless of which one is configured first. Previously, whichever was registered last won, so a `ReverseMap()` call could silently overwrite an explicit map configured earlier in the same profile. Two explicit `CreateMap` calls for the same pair are unaffected — the one registered last still wins.

## [1.1.0] — —

### Changed

- Updated `README.md` documentation.

## [1.0.2] — —

### Added

- Initial NuGet release with Profile-based and Attribute-based mapping support.
- Profile registration: `Profile.Configure(IMapperConfigurationExpression)`, `Mapper.CreateMap<TProfile>()`, `IMapper`.
- Map declaration: `IMapperConfigurationExpression.CreateMap<TSource, TDestination>(Action<IMappingExpression<…>>)`.
- Convention-based mapping with case-insensitive property-name matching.
- Member configuration via `ForMember` with `MapFrom`, `Ignore`, `NullSubstitute`.
- Reverse mapping via `IMappingExpression.ReverseMap()`.
- Nested and list mapping support.
- Configuration validation via `Mapper.AssertConfigurationIsValid()`.
- Static attribute-based mapper: `ModelMapper.Map<TSource, TTarget>()` with `[PropertyMapping("Name")]`.
- Multi-targeting: net6.0, net7.0, net8.0, net9.0.

[Unreleased]: https://github.com/kadirdemirkaya/MapFlux/compare/41fe862ef14cfe18db6076ef15e1802d1e9a94c1...HEAD
[1.1.0]: https://github.com/kadirdemirkaya/MapFlux/compare/abac68926181f6de0dcb64cfdbbe0b69eed7e6fc...41fe862ef14cfe18db6076ef15e1802d1e9a94c1
[1.0.2]: https://github.com/kadirdemirkaya/MapFlux/commit/abac68926181f6de0dcb64cfdbbe0b69eed7e6fc
