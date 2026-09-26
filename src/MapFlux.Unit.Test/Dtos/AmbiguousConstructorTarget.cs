namespace MapFlux.Unit.Test.Dtos
{
    public class AmbiguousConstructorTarget
    {
        public string Name { get; }
        public int Age { get; }

        public AmbiguousConstructorTarget(string name)
        {
            Name = name;
        }

        public AmbiguousConstructorTarget(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }
}
