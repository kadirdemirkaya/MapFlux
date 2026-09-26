using System.Collections.Generic;

namespace MapFlux.Unit.Test.Models
{
    public class ConstructorParentSource
    {
        public ConstructorChildSource Child { get; set; }
        public List<ConstructorChildSource> Children { get; set; }
    }
}
