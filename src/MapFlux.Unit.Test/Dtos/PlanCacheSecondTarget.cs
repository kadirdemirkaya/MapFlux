namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheSecondTarget
    {
        [PropertyMapping("Label")]
        public string Caption { get; set; } = null!;

        public int Count { get; } = 7;

        public PlanCacheChildTarget Child { get; set; } = null!;
    }
}
