using System.Linq.Expressions;

namespace MapFlux
{
    internal static class ValueConversion
    {
        internal static Expression? TryConvert(Expression value, Type destinationType)
        {
            var sourceType = value.Type;

            if (sourceType == destinationType)
            {
                return value;
            }

            if (destinationType.IsAssignableFrom(sourceType))
            {
                return Expression.Convert(value, destinationType);
            }

            var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
            var destinationUnderlying = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            if (destinationUnderlying.IsAssignableFrom(sourceUnderlying))
            {
                return Expression.Convert(value, destinationType);
            }

            if (!TypeCompatibility.IsNumericOrEnum(sourceUnderlying) ||
                !TypeCompatibility.IsNumericOrEnum(destinationUnderlying))
            {
                return null;
            }

            Expression converted = sourceType == sourceUnderlying
                ? value
                : Expression.Convert(value, sourceUnderlying);

            if (sourceUnderlying.IsEnum)
            {
                converted = Expression.Convert(converted, Enum.GetUnderlyingType(sourceUnderlying));
            }

            converted = Expression.Convert(converted, destinationUnderlying.IsEnum
                ? Enum.GetUnderlyingType(destinationUnderlying)
                : destinationUnderlying);

            if (destinationUnderlying.IsEnum)
            {
                converted = Expression.Convert(converted, destinationUnderlying);
            }

            return destinationType == destinationUnderlying
                ? converted
                : Expression.Convert(converted, destinationType);
        }

        internal static Func<object, object>? CreateBoxedConverter(Type sourceType, Type destinationType)
        {
            if (!TypeCompatibility.IsNumericOrEnum(sourceType) || !TypeCompatibility.IsNumericOrEnum(destinationType))
            {
                return null;
            }

            var valueParameter = Expression.Parameter(typeof(object), "value");
            var converted = TryConvert(Expression.Convert(valueParameter, sourceType), destinationType);

            if (converted is null)
            {
                return null;
            }

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(converted, typeof(object)), valueParameter).Compile();
        }
    }
}
