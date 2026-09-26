using System.Collections.Generic;

namespace MapFlux.Unit.Test.Dtos
{
    public record ConstructorParentTarget(ConstructorChildTarget Child, List<ConstructorChildTarget> Children);
}
