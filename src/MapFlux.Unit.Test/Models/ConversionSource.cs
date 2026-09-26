namespace MapFlux.Unit.Test.Models
{
    public class ConversionSource
    {
        public int Count { get; set; }
        public long Total { get; set; }
        public int Amount { get; set; }
        public float Ratio { get; set; }
        public int? OptionalCount { get; set; }
        public int Score { get; set; }
        public int Code { get; set; }
        public ConversionLevel Level { get; set; }
        public ConversionLevel Priority { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
