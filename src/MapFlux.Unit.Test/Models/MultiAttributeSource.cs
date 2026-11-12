namespace MapFlux.Unit.Test.Models;

public class MultiAttributeSource
{
    public int Id { get; set; }

    [PropertyMapping("InternalReference")]
    public string InternalCode { get; set; } = null!;

    [PropertyMapping("ExternalReference")]
    public string ExternalCode { get; set; } = null!;
}
