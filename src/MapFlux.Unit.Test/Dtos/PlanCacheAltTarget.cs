namespace MapFlux.Unit.Test.Dtos
{
    public class PlanCacheAltTarget
    {
        public string Label { get; set; }
        public PlanCacheAltChildTarget Child { get; set; }
    }

    public class PlanCacheAltChildTarget
    {
        [PropertyMapping("Note")]
        public string Remark { get; set; }
    }
}
