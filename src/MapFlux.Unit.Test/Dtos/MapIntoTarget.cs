using System.Collections.Generic;

namespace MapFlux.Unit.Test.Dtos
{
    public class MapIntoTarget
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }
        public string Untouched { get; set; }
        public MapIntoChildTarget Child { get; set; }
        public List<ElementTarget> Items { get; set; }
    }

    public class MapIntoChildTarget
    {
        public string Note { get; set; }
        public int Version { get; set; }
    }
}
