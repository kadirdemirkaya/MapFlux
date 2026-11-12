namespace MapFlux.Unit.Test.Models
{
    public class RuntimeTypeSource
    {
        public string Title { get; set; } = null!;
        public RuntimeTypeDetailSource Detail { get; set; } = null!;
    }

    public class RuntimeTypeDetailSource
    {
        public string Note { get; set; } = null!;
    }

    public class RuntimeTypeFirstDetailSource : RuntimeTypeDetailSource
    {
        public string Extra { get; set; } = null!;
    }

    public class RuntimeTypeSecondDetailSource : RuntimeTypeDetailSource
    {
        public string Marker { get; set; } = null!;
    }
}
