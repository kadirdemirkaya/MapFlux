using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    internal sealed class MemberValueConverter
    {
        internal static readonly ConstructorInfo InvalidOperationExceptionConstructor =
            typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!;

        private static readonly Func<object, object> _passThrough = value => value;

        private readonly ConcurrentDictionary<Type, Func<object, object>?> _runtimeConverters = new();
        private readonly Type _destinationType;
        private readonly Type _declaredSourceType;
        private readonly Func<object, object>? _declaredConverter;
        private readonly string _destinationTypeName;
        private readonly string _memberName;

        private MemberValueConverter(
            Type destinationType,
            Type declaredSourceType,
            Func<object, object>? declaredConverter,
            string destinationTypeName,
            string memberName)
        {
            _destinationType = destinationType;
            _declaredSourceType = declaredSourceType;
            _declaredConverter = declaredConverter;
            _destinationTypeName = destinationTypeName;
            _memberName = memberName;
        }

        internal static MemberValueConverter? Create(
            PropertyInfo destinationProperty, Type sourceType, string destinationTypeName)
        {
            var destinationType = Nullable.GetUnderlyingType(destinationProperty.PropertyType)
                ?? destinationProperty.PropertyType;
            var declaredSourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;

            if (destinationType.IsAssignableFrom(declaredSourceType))
            {
                return null;
            }

            return new MemberValueConverter(
                destinationType,
                declaredSourceType,
                ValueConversion.CreateBoxedConverter(declaredSourceType, destinationType),
                destinationTypeName,
                destinationProperty.Name);
        }

        internal static string UnconvertibleMessage(
            string destinationTypeName, string memberName, string valueTypeName, string destinationMemberTypeName)
        {
            return $"Cannot map {destinationTypeName}.{memberName}: " +
                   $"no conversion from {valueTypeName} to {destinationMemberTypeName} is available. " +
                   $"Register a map for that type pair with CreateMap, or use ForMember with MapFrom " +
                   $"to supply a {destinationMemberTypeName} value.";
        }

        internal object Convert(object value)
        {
            var valueType = value.GetType();

            if (valueType == _declaredSourceType)
            {
                return _declaredConverter is null
                    ? throw UnconvertibleValue(valueType)
                    : _declaredConverter(value);
            }

            var runtimeConverter = _runtimeConverters.GetOrAdd(valueType, type => _destinationType.IsAssignableFrom(type)
                ? _passThrough
                : ValueConversion.CreateBoxedConverter(type, _destinationType));

            return runtimeConverter is null
                ? throw UnconvertibleValue(valueType)
                : runtimeConverter(value);
        }

        private InvalidOperationException UnconvertibleValue(Type valueType)
        {
            return new InvalidOperationException(
                UnconvertibleMessage(_destinationTypeName, _memberName, valueType.Name, _destinationType.Name));
        }
    }
}
