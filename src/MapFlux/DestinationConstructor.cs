using System.Collections.Concurrent;
using System.Reflection;

namespace MapFlux
{
    internal sealed class DestinationConstructor
    {
        private static readonly ConcurrentDictionary<Type, DestinationConstructor?> _bindings = new();

        private static readonly Func<Type, DestinationConstructor?> _findBinding = FindBinding;

        private DestinationConstructor(
            ConstructorInfo constructor, IReadOnlyList<(ParameterInfo Parameter, PropertyInfo Member)> bindings)
        {
            Constructor = constructor;
            Bindings = bindings;
            BoundMemberNames = new HashSet<string>(bindings.Select(binding => binding.Member.Name));
        }

        internal ConstructorInfo Constructor { get; }

        internal IReadOnlyList<(ParameterInfo Parameter, PropertyInfo Member)> Bindings { get; }

        internal HashSet<string> BoundMemberNames { get; }

        internal static DestinationConstructor? Resolve(Type sourceType, Type destinationType)
        {
            if (destinationType.GetConstructor(Type.EmptyTypes) is not null)
            {
                return null;
            }

            return TryBind(destinationType)
                ?? throw new InvalidOperationException(BuildFailureMessage(sourceType, destinationType));
        }

        internal static bool IsBindable(Type destinationType) => TryBind(destinationType) is not null;

        private static DestinationConstructor? TryBind(Type destinationType) =>
            _bindings.GetOrAdd(destinationType, _findBinding);

        private static DestinationConstructor? FindBinding(Type destinationType)
        {
            DestinationConstructor? match = null;

            foreach (var constructor in destinationType.GetConstructors())
            {
                if (!TryBindConstructor(destinationType, constructor, out var candidate))
                {
                    continue;
                }

                if (match is not null)
                {
                    return null;
                }

                match = candidate;
            }

            return match;
        }

        private static bool TryBindConstructor(
            Type destinationType, ConstructorInfo constructor, out DestinationConstructor? binding)
        {
            binding = null;

            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                return false;
            }

            var members = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var bindings = new List<(ParameterInfo, PropertyInfo)>(parameters.Length);

            foreach (var parameter in parameters)
            {
                var member = FindMember(members, parameter.Name);

                if (member is null)
                {
                    return false;
                }

                bindings.Add((parameter, member));
            }

            binding = new DestinationConstructor(constructor, bindings);
            return true;
        }

        private static PropertyInfo? FindMember(PropertyInfo[] members, string? parameterName)
        {
            if (parameterName is null)
            {
                return null;
            }

            foreach (var member in members)
            {
                if (member.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return member;
                }
            }

            return null;
        }

        private static string BuildFailureMessage(Type sourceType, Type destinationType)
        {
            var prefix = $"CreateMap<{sourceType.Name}, {destinationType.Name}> failed: " +
                         $"{destinationType.Name} does not have a public parameterless constructor";

            var constructors = destinationType.GetConstructors();

            if (constructors.Length == 0)
            {
                return prefix + " and no public constructor at all, so MapFlux cannot create instances of it. " +
                       "Add a parameterless constructor, or map to a destination type that has one.";
            }

            var matching = constructors
                .Where(constructor => TryBindConstructor(destinationType, constructor, out _))
                .ToList();

            if (matching.Count > 1)
            {
                return prefix + ", and more than one of its public constructors matches its members: " +
                       string.Join(", ", matching.Select(constructor => Describe(destinationType, constructor))) + ". " +
                       "Leave a single constructor whose parameter names match the destination members, " +
                       "or add a parameterless constructor.";
            }

            var members = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var unusable = constructors.Select(constructor =>
                $"{Describe(destinationType, constructor)} has no member for " +
                string.Join(", ", constructor.GetParameters()
                    .Where(parameter => FindMember(members, parameter.Name) is null)
                    .Select(parameter => $"'{parameter.Name}'")));

            return prefix + ", and none of its public constructors can be used to build it: " +
                   string.Join("; ", unusable) + ". " +
                   $"Name the constructor parameters after the members of {destinationType.Name} " +
                   "(case-insensitive), or add a parameterless constructor.";
        }

        private static string Describe(Type destinationType, ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters()
                .Select(parameter => $"{CollectionMapper.Describe(parameter.ParameterType)} {parameter.Name}");

            return $"{destinationType.Name}({string.Join(", ", parameters)})";
        }
    }
}
