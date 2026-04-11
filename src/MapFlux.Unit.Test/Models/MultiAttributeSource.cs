namespace MapFlux.Unit.Test.Models;

public class MultiAttributeSource
{
    public int Id { get; set; }

    [PropertyMapping("InternalReference")]
    public string InternalCode { get; set; }

    [PropertyMapping("ExternalReference")]
    public string ExternalCode { get; set; }
}
