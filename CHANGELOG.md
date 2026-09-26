# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

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
