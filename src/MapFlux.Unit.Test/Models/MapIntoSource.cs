using System.Collections.Generic;

namespace MapFlux.Unit.Test.Models
{
    public class MapIntoSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Secret { get; set; } = null!;
        public MapIntoChildSource Child { get; set; } = null!;
        public List<ElementSource> Items { get; set; } = null!;
    }

    public class MapIntoChildSource
    {
        public string Note { get; set; } = null!;
        public int Version { get; set; }
    }
}
