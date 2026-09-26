namespace MapFlux
{
    internal static class MappingDepth
    {
        internal const int DefaultLimit = 32;

        internal const string ExceededMarker = "MapFlux.MaxDepthExceeded";

        [ThreadStatic]
        private static int _depth;

        internal static void Enter(int limit, Type sourceType, Type destinationType)
        {
            if (_depth >= limit)
            {
                throw Exceeded(limit, sourceType, destinationType);
            }

            _depth++;
        }

        internal static void Exit() => _depth--;

        internal static bool WasExceeded(Exception exception) =>
            exception is InvalidOperationException && exception.Data.Contains(ExceededMarker);

        internal static int Validate(int value, string memberName)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "value", value, $"{memberName} must be at least 1.");
            }

            return value;
        }

        private static InvalidOperationException Exceeded(int limit, Type sourceType, Type destinationType)
        {
            var exception = new InvalidOperationException(
                $"Mapping from {sourceType.Name} to {destinationType.Name} exceeded the maximum depth of {limit}. " +
                "The source graph is either cyclic, meaning an object that references itself directly or " +
                "indirectly, or deeper than the configured limit. Break the cycle with Ignore on the member " +
                "that closes it, or raise the limit with Mapper.MaxDepth or ModelMapper.MaxDepth.");

            exception.Data[ExceededMarker] = true;

            return exception;
        }
    }
}
