using System.Linq.Expressions;

namespace MapFlux
{
    internal interface IProjectableMap
    {
        Type SourceType { get; }

        Type DestinationType { get; }

        DestinationConstructor? DestinationConstructor { get; }

        bool IsIgnored(string destinationName);

        LambdaExpression? FindMemberMapping(string destinationName);

        bool TryGetNullSubstitute(string destinationName, out object? value, out Type memberType);
    }
}
