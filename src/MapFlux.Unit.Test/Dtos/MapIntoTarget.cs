using System.Collections.Generic;

namespace MapFlux.Unit.Test.Dtos
{
    public class MapIntoTarget
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public string Untouched { get; set; } = null!;
        public MapIntoChildTarget Child { get; set; } = null!;
        public List<ElementTarget> Items { get; set; } = null!;
    }

    public class MapIntoChildTarget
    {
        public string Note { get; set; } = null!;
        public int Version { get; set; }
    }
}
