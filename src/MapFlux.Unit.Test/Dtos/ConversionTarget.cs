using MapFlux.Unit.Test.Models;

namespace MapFlux.Unit.Test.Dtos
{
    public class ConversionTarget
    {
        public long Count { get; set; }
        public int Total { get; set; }
        public decimal Amount { get; set; }
        public double Ratio { get; set; }
        public long OptionalCount { get; set; }
        public long? Score { get; set; }
        public int? Code { get; set; }
        public int Level { get; set; }
        public long Priority { get; set; }
        public ConversionLevel Rank { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
