namespace MapFlux.Unit.Test.Models
{
    public class RuntimeTypeSource
    {
        public string Title { get; set; }
        public RuntimeTypeDetailSource Detail { get; set; }
    }

    public class RuntimeTypeDetailSource
    {
        public string Note { get; set; }
    }

    public class RuntimeTypeFirstDetailSource : RuntimeTypeDetailSource
    {
        public string Extra { get; set; }
    }

    public class RuntimeTypeSecondDetailSource : RuntimeTypeDetailSource
    {
        public string Marker { get; set; }
    }
}
