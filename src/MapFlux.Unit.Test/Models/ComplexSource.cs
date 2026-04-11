using System.Collections.Generic;

namespace MapFlux.Unit.Test.Models
{
    public class ComplexSource
    {
        public int Id { get; set; }
        public List<string> Items { get; set; }
        public NestedSource Nested { get; set; }
    }

    public class NestedSource
    {
        public string Value { get; set; }
    }
}
