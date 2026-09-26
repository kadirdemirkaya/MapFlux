using System.Reflection;

namespace MapFlux
{
    internal sealed class MemberMapper
    {
        internal static readonly MethodInfo MapMethod =
            typeof(MemberMapper).GetMethod(nameof(Map), BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly Func<object, object> _passThrough = value => value;

        private readonly Mapper _mapper;
        private readonly PropertyInfo _destinationProperty;
        private readonly MemberValueConverter? _converter;

        private volatile Resolution? _resolution;

        internal MemberMapper(Mapper mapper, PropertyInfo destinationProperty, MemberValueConverter? converter)
        {
            _mapper = mapper;
            _destinationProperty = destinationProperty;
            _converter = converter;
        }

        internal object Map(object value)
        {
            var valueType = value.GetType();
            var registrationVersion = _mapper.RegistrationVersion;
            var resolution = _resolution;

            if (resolution is null ||
                resolution.ValueType != valueType ||
                resolution.RegistrationVersion != registrationVersion)
            {
                resolution = new Resolution(valueType, registrationVersion, Resolve(valueType));
                _resolution = resolution;
            }

            return resolution.MapValue(value);
        }

        private Func<object, object> Resolve(Type valueType)
        {
            var destinationType = _destinationProperty.PropertyType;

            var collectionOutcome = CollectionMapper.TryResolve(
                valueType, destinationType, _mapper, _destinationProperty, out var buildCollection);

            if (collectionOutcome == CollectionMapOutcome.Mapped)
            {
                return buildCollection!;
            }

            if (_mapper._mappings.TryGetValue((valueType, destinationType), out var nestedMapping))
            {
                return nestedMapping;
            }

            return _converter is null ? _passThrough : _converter.Convert;
        }

        private sealed class Resolution
        {
            private readonly Func<object, object> _map;

            internal Resolution(Type valueType, int registrationVersion, Func<object, object> map)
            {
                ValueType = valueType;
                RegistrationVersion = registrationVersion;
                _map = map;
            }

            internal Type ValueType { get; }

            internal int RegistrationVersion { get; }

            internal object MapValue(object value) => _map(value);
        }
    }
}
