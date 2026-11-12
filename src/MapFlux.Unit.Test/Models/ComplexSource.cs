using System.Collections.Generic;

namespace MapFlux.Unit.Test.Models
{
    public class ComplexSource
    {
        public int Id { get; set; }
        public List<string> Items { get; set; } = null!;
        public NestedSource Nested { get; set; } = null!;
    }

    public class NestedSource
    {
        public string Value { get; set; } = null!;
    }
}
