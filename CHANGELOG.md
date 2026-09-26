# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- `Mapper.AssertConfigurationIsValid(bool strict)` overload: `strict: true` additionally validates every registered mapping's nested and element maps and member type compatibility (including the numeric/`Nullable<T>`/enum conversions above), aggregating every problem into one `InvalidOperationException`. The existing `AssertConfigurationIsValid()` overload is unchanged and keeps validating only unmapped destination properties.
- `Mapper.Map` now maps any source `IEnumerable<T>` other than `string` — an array, a `List<T>`, a collection interface, a LINQ result — into `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>` and `IReadOnlyList<T>`, both for a member and for a top-level `Map` call. A registered element map is applied to every element; otherwise the source instance is passed through when the destination type already accepts it, or the elements are copied into the destination shape when the source element type is assignable. Previously only `List<T>` to `List<T>` worked, and an array, `IEnumerable<T>` or `IReadOnlyList<T>` destination failed. A `null` collection stays `null` and a `null` element is kept as `null`, as for a `List<T>`.

### Fixed

- `CreateMap` for a destination type without a public parameterless constructor now throws a descriptive `InvalidOperationException` naming the destination type, right when `CreateMap` is called. Previously it threw a `TypeInitializationException` and permanently poisoned that source/destination pair for the rest of the process, so even a later, correct registration for the same pair kept failing.
- A collection member or a top-level collection call whose elements can be neither mapped nor assigned now throws an `InvalidOperationException` naming the member and both element types, instead of reporting the two collection type names.

- `Mapper.Map` no longer throws `InvalidCastException` when a source and a destination member have different types. Numeric conversions in both directions, `Nullable<T>` in both directions, and enum to or from its underlying numeric type are now performed inside the compiled mapping plan — both for name-matched members and for `MapFrom`. A `null` source member still leaves the destination member at the default value of its type.
- A member pair with no available conversion, such as `string` to `int`, now throws an `InvalidOperationException` naming the member and both type names instead of a bare `InvalidCastException`. Configuration and `AssertConfigurationIsValid()` stay silent about such a pair, so registering a map never fails at startup.
- `Mapper.Map` no longer throws `NullReferenceException` for a `null` source, a `null` source list, or a `null` element inside a list (top-level or nested property). A `null` source now returns `default(TDestination)`, a `null` list returns `null`, and a `null` list element is kept as `null` in the mapped list.
- `ForMember` no longer throws `InvalidCastException` for a destination expression wrapped in a conversion, such as `d => (object)d.Name`.
- `ForMember` now throws a descriptive `ArgumentException` at configuration time for a destination expression that is not a direct member access on the destination parameter — including a nested path such as `d => d.Inner.Name`. Previously a nested path silently wrote the mapped value to the wrong top-level member instead of the intended nested one.

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
