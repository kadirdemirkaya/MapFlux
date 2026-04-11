namespace MapFlux.Unit.Test.Dtos;

public class MultiAttributeTarget
{
    public int Id { get; set; }

    [PropertyMapping("InternalCode")]
    public string InternalReference { get; set; }

    [PropertyMapping("ExternalCode")]
    public string ExternalReference { get; set; }
}
