namespace MapFlux.Unit.Test.Dtos
{
    public class RuntimeTypeTarget
    {
        public string Title { get; set; } = null!;
        public RuntimeTypeDetailTarget Detail { get; set; } = null!;
    }

    public class RuntimeTypeDetailTarget
    {
        public string Note { get; set; } = null!;
        public string Extra { get; set; } = null!;
    }
}
