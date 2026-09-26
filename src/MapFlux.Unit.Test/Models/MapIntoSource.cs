using System.Collections.Generic;

namespace MapFlux.Unit.Test.Models
{
    public class MapIntoSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }
        public MapIntoChildSource Child { get; set; }
        public List<ElementSource> Items { get; set; }
    }

    public class MapIntoChildSource
    {
        public string Note { get; set; }
        public int Version { get; set; }
    }
}
