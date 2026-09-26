namespace MapFlux.Unit.Test.Dtos
{
    public record ConstructorTarget(string Name, long Age)
    {
        public string Nickname { get; set; }
    }
}
