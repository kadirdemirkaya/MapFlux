namespace MapFlux.Unit.Test.Dtos
{
    public class ReadOnlyConstructorTarget
    {
        public string Name { get; }
        public long Age { get; }

        public ReadOnlyConstructorTarget(string name, long age)
        {
            Name = name;
            Age = age;
        }
    }
}
