namespace MapFlux.Unit.Test.Models
{
    public class CycleNodeSource
    {
        public int Id { get; set; }
        public CycleNodeSource Next { get; set; } = null!;
        public List<CycleNodeSource> Children { get; set; } = null!;
    }
}
