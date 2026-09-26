# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

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
