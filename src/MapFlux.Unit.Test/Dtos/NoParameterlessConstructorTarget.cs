namespace MapFlux.Unit.Test.Dtos
{
    public class NoParameterlessConstructorTarget
    {
        public string Name { get; set; }

        public NoParameterlessConstructorTarget(string name)
        {
            Name = name;
        }
    }
}
