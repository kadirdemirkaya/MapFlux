using System.Reflection;

namespace MapFlux.DependencyInjection
{
    internal static class AssemblyArguments
    {
        internal static Assembly[] Validate(Assembly[] assemblies)
        {
            if (assemblies is null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            if (assemblies.Length == 0)
            {
                throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));
            }

            if (assemblies.Any(assembly => assembly is null))
            {
                throw new ArgumentException("The assemblies must not contain null.", nameof(assemblies));
            }

            return (Assembly[])assemblies.Clone();
        }
    }
}
