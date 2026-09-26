namespace MapFlux.Unit.Test.Dtos
{
    public class CycleNodeTarget
    {
        public int Id { get; set; }
        public CycleNodeTarget Next { get; set; }
        public List<CycleNodeTarget> Children { get; set; }
    }
}
