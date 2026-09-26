namespace MapFlux.Unit.Test.Dtos
{
    public class RuntimeTypeTarget
    {
        public string Title { get; set; }
        public RuntimeTypeDetailTarget Detail { get; set; }
    }

    public class RuntimeTypeDetailTarget
    {
        public string Note { get; set; }
        public string Extra { get; set; }
    }
}
