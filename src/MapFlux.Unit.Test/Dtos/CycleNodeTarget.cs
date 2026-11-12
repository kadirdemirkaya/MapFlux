namespace MapFlux.Unit.Test.Dtos
{
    public class CycleNodeTarget
    {
        public int Id { get; set; }
        public CycleNodeTarget Next { get; set; } = null!;
        public List<CycleNodeTarget> Children { get; set; } = null!;
    }
}
