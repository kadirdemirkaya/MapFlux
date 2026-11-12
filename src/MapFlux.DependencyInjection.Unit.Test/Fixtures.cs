namespace MapFlux.DependencyInjection.Unit.Test
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class Order
    {
        public int Number { get; set; }

        public Customer Customer { get; set; } = new();
    }

    public class OrderDto
    {
        public int Number { get; set; }

        public CustomerDto Customer { get; set; } = new();
    }

    public class CustomerSummary
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Unmatched { get; set; } = string.Empty;
    }

    public class Node
    {
        public int Value { get; set; }

        public Node? Next { get; set; }
    }

    public class NodeDto
    {
        public int Value { get; set; }

        public NodeDto? Next { get; set; }
    }

    public class CustomerProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Customer, CustomerDto>(m => { });
        }
    }

    public class OrderProfile : Profile
    {
        public override void Configure(IMapperConfigurationExpression config)
        {
            config.CreateMap<Order, OrderDto>(m => { });
        }
    }
}
