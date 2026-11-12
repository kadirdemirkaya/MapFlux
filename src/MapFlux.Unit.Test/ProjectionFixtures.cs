namespace MapFlux.Unit.Test.Projection
{
    public enum ProjectionStatus
    {
        Active = 1,
        Closed = 2
    }

    public class ProjectionCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public int? Score { get; set; }
        public decimal Total { get; set; }
        public ProjectionStatus Status { get; set; }
        public string Secret { get; set; } = null!;
        public ProjectionAddress? Address { get; set; }
        public List<ProjectionLine> Lines { get; set; } = new();
    }

    public class ProjectionAddress
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    public class ProjectionLine
    {
        public string Product { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class ProjectionCustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string? Email { get; set; }
        public int Score { get; set; }
        public double Total { get; set; }
        public int Status { get; set; }
        public string Secret { get; set; } = "untouched";
        public ProjectionAddressDto? Address { get; set; }
        public List<ProjectionLineDto> Lines { get; set; } = null!;
    }

    public class ProjectionAddressDto
    {
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
    }

    public class ProjectionLineDto
    {
        public string Product { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class ProjectionCaseDto
    {
        public int ID { get; set; }
        public string NAME { get; set; } = null!;
    }

    public class ProjectionShapesSource
    {
        public List<ProjectionLine> Lines { get; set; } = new();
        public List<int> Numbers { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    public class ProjectionShapesDto
    {
        public ProjectionLineDto[] Lines { get; set; } = null!;
        public int[] Numbers { get; set; } = null!;
        public List<string> Tags { get; set; } = null!;
    }

    public class ProjectionInterfaceShapesDto
    {
        public IEnumerable<ProjectionLineDto> Lines { get; set; } = null!;
        public IReadOnlyList<int> Numbers { get; set; } = null!;
        public IList<string> Tags { get; set; } = null!;
    }

    public class ProjectionUnsupportedCollectionDto
    {
        public System.Collections.ObjectModel.ObservableCollection<ProjectionLineDto> Lines { get; set; } = null!;
    }

    public record ProjectionPersonRecord(int Id, string Name)
    {
        public string? Email { get; set; }
    }

    public class ProjectionNullableSource
    {
        public string? Nickname { get; set; }
        public int? Rank { get; set; }
        public ProjectionAddress? Address { get; set; }
    }

    public class ProjectionNullableDto
    {
        public string? Nickname { get; set; } = "initial";
        public int? Rank { get; set; } = 7;
        public ProjectionAddressDto? Address { get; set; } = new() { City = "initial", Street = "initial" };
    }

    public class ProjectionSubstituteDto
    {
        public string Nickname { get; set; } = null!;
        public int Rank { get; set; }
    }

    public class ProjectionUnconvertibleSource
    {
        public string Code { get; set; } = null!;
        public Guid Key { get; set; }
        public int Id { get; set; }
    }

    public class ProjectionUnconvertibleDto
    {
        public int Code { get; set; }
        public DateTime Key { get; set; }
        public int Id { get; set; }
    }

    public class ProjectionDictionarySource
    {
        public Dictionary<string, ProjectionLine> Items { get; set; } = new();
    }

    public class ProjectionDictionaryDto
    {
        public Dictionary<string, ProjectionLineDto> Items { get; set; } = null!;
    }

    public class ProjectionAnimal
    {
        public string Name { get; set; } = null!;
    }

    public class ProjectionDog : ProjectionAnimal
    {
        public string Breed { get; set; } = null!;
    }

    public class ProjectionAnimalDto
    {
        public string Name { get; set; } = null!;
    }

    public class ProjectionDogDto : ProjectionAnimalDto
    {
        public string Breed { get; set; } = null!;
    }

    public class ProjectionOwner
    {
        public ProjectionAnimal Pet { get; set; } = null!;
    }

    public class ProjectionOwnerDto
    {
        public ProjectionAnimalDto Pet { get; set; } = null!;
    }
}
