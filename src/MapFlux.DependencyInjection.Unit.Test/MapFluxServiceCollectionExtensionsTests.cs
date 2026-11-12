using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MapFlux.DependencyInjection.Unit.Test
{
    public class MapFluxServiceCollectionExtensionsTests
    {
        private static ServiceProvider BuildProvider(Action<IServiceCollection> register)
        {
            var services = new ServiceCollection();
            register(services);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void AddMapFlux_WithConfigurationCallback_ResolvesMapperThatMaps()
        {
            using var provider = BuildProvider(services =>
                services.AddMapFlux(cfg => cfg.CreateMap<Customer, CustomerDto>(m => { })));

            var mapper = provider.GetRequiredService<IMapper>();
            var dto = mapper.Map<Customer, CustomerDto>(new Customer { Id = 7, Name = "Ada" });

            Assert.Equal(7, dto.Id);
            Assert.Equal("Ada", dto.Name);
        }

        [Fact]
        public void AddMapFlux_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();

            var result = services.AddMapFlux(typeof(CustomerProfile).Assembly);

            Assert.Same(services, result);
        }

        [Fact]
        public void AddMapFlux_RegistersMapperAsSingletonAcrossScopes()
        {
            using var provider = BuildProvider(services => services.AddMapFlux(typeof(CustomerProfile).Assembly));

            var root = provider.GetRequiredService<IMapper>();
            using var scope = provider.CreateScope();
            var scoped = scope.ServiceProvider.GetRequiredService<IMapper>();

            Assert.Same(root, scoped);
            Assert.Same(root, provider.GetRequiredService<IMapper>());
        }

        [Fact]
        public void AddMapFlux_BothInterfacesResolveSameInstance_AndConcreteTypeIsNotRegistered()
        {
            using var provider = BuildProvider(services => services.AddMapFlux(typeof(CustomerProfile).Assembly));

            var mapper = provider.GetRequiredService<IMapper>();
            var existingDestinationMapper = provider.GetRequiredService<IExistingDestinationMapper>();

            Assert.Same(mapper, existingDestinationMapper);
            Assert.Null(provider.GetService<Mapper>());
        }

        [Fact]
        public void AddMapFlux_ExistingDestinationMapperUsesTheRegisteredMaps()
        {
            using var provider = BuildProvider(services => services.AddMapFlux(typeof(CustomerProfile).Assembly));
            var destination = new CustomerDto { Id = 1, Name = "Old" };

            var result = provider.GetRequiredService<IExistingDestinationMapper>()
                .Map(new Customer { Id = 2, Name = "New" }, destination);

            Assert.Same(destination, result);
            Assert.Equal(2, destination.Id);
            Assert.Equal("New", destination.Name);
        }

        [Fact]
        public void AddMapFlux_WithAssemblies_RegistersEveryProfileInThem()
        {
            using var provider = BuildProvider(services => services.AddMapFlux(typeof(CustomerProfile).Assembly));

            var dto = provider.GetRequiredService<IMapper>().Map<Order, OrderDto>(
                new Order { Number = 3, Customer = new Customer { Id = 4, Name = "Grace" } });

            Assert.Equal(3, dto.Number);
            Assert.Equal(4, dto.Customer.Id);
            Assert.Equal("Grace", dto.Customer.Name);
        }

        [Fact]
        public void AddMapFlux_WithNoAssembly_ThrowsTheSameArgumentExceptionAsTheMapper()
        {
            var services = new ServiceCollection();

            var fromServices = Assert.Throws<ArgumentException>(() => services.AddMapFlux());
            var fromOptions = Assert.Throws<ArgumentException>(() =>
                services.AddMapFlux((MapFluxOptions options) => options.AddProfilesFromAssemblies()));
            var fromMapper = Assert.Throws<ArgumentException>(() => new Mapper().CreateMapsFromAssemblies());

            Assert.Equal(fromMapper.Message, fromServices.Message);
            Assert.Equal(fromMapper.Message, fromOptions.Message);
            Assert.Equal("assemblies", fromServices.ParamName);
            Assert.Empty(services);
        }

        [Fact]
        public void AddMapFlux_WithNullAssemblyInTheList_ThrowsArgumentException()
        {
            var services = new ServiceCollection();

            var exception = Assert.Throws<ArgumentException>(() =>
                services.AddMapFlux(typeof(CustomerProfile).Assembly, null!));

            Assert.Equal("assemblies", exception.ParamName);
            Assert.Empty(services);
        }

        [Fact]
        public void AddMapFlux_WithOptions_AppliesProfilesAndInlineMaps()
        {
            using var provider = BuildProvider(services => services.AddMapFlux((MapFluxOptions options) => options
                .AddProfile<CustomerProfile>()
                .Configure(cfg => cfg.CreateMap<Order, OrderDto>(m => m.ForMember(d => d.Number, o => o.Ignore())))));

            var mapper = provider.GetRequiredService<IMapper>();
            var dto = mapper.Map<Order, OrderDto>(new Order { Number = 9, Customer = new Customer { Id = 5, Name = "Linus" } });

            Assert.Equal(0, dto.Number);
            Assert.Equal(5, dto.Customer.Id);
            Assert.Equal("Linus", dto.Customer.Name);
        }

        [Fact]
        public void AddMapFlux_WithOptionsMaxDepth_AppliesItToTheMapper()
        {
            using var provider = BuildProvider(services => services.AddMapFlux((MapFluxOptions options) =>
            {
                options.MaxDepth = 2;
                options.Configure(cfg => cfg.CreateMap<Node, NodeDto>(m => { }));
            }));

            var mapper = provider.GetRequiredService<IMapper>();
            var deep = new Node { Value = 1, Next = new Node { Value = 2, Next = new Node { Value = 3 } } };

            Assert.Equal(2, ((Mapper)mapper).MaxDepth);
            Assert.Equal(1, mapper.Map<Node, NodeDto>(new Node { Value = 1, Next = new Node { Value = 2 } }).Value);
            Assert.Throws<InvalidOperationException>(() => mapper.Map<Node, NodeDto>(deep));
        }

        [Fact]
        public void MapFluxOptions_MaxDepth_DefaultsToTheMapperDefault_AndRejectsValuesBelowOne()
        {
            var options = new MapFluxOptions();

            Assert.Equal(new Mapper().MaxDepth, options.MaxDepth);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Mapper().MaxDepth = 0);
            Assert.Equal(new Mapper().MaxDepth, options.MaxDepth);
        }

        [Fact]
        public void AddMapFlux_CalledTwice_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            services.AddMapFlux(typeof(CustomerProfile).Assembly);
            var registrations = services.Count;

            var callback = Assert.Throws<InvalidOperationException>(() =>
                services.AddMapFlux(cfg => cfg.CreateMap<Customer, CustomerDto>(m => { })));
            var assemblies = Assert.Throws<InvalidOperationException>(() =>
                services.AddMapFlux(typeof(CustomerProfile).Assembly));
            var options = Assert.Throws<InvalidOperationException>(() =>
                services.AddMapFlux((MapFluxOptions o) => o.AddProfile<CustomerProfile>()));

            Assert.Contains("AddMapFlux has already been called", callback.Message);
            Assert.Equal(callback.Message, assemblies.Message);
            Assert.Equal(callback.Message, options.Message);
            Assert.Equal(registrations, services.Count);
        }

        [Fact]
        public void AddMapFlux_WithoutValidation_ResolvesEvenWhenAMemberIsUnmapped()
        {
            using var provider = BuildProvider(services =>
                services.AddMapFlux(cfg => cfg.CreateMap<Customer, CustomerSummary>(m => { })));

            var summary = provider.GetRequiredService<IMapper>().Map<Customer, CustomerSummary>(new Customer { Id = 1 });

            Assert.Equal(1, summary.Id);
        }

        [Fact]
        public void AddMapFlux_WithValidation_ThrowsOnResolveWhenAMemberIsUnmapped()
        {
            using var provider = BuildProvider(services => services.AddMapFlux((MapFluxOptions options) =>
            {
                options.ValidateConfiguration = true;
                options.Configure(cfg => cfg.CreateMap<Customer, CustomerSummary>(m => { }));
            }));

            var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMapper>());

            Assert.Contains("Unmatched", exception.Message);
        }

        [Fact]
        public void AddMapFlux_WithStrictValidation_RequiresTheNestedMap()
        {
            Action<MapFluxOptions> Options(bool strict) => options =>
            {
                options.ValidateConfiguration = true;
                options.StrictValidation = strict;
                options.AddProfile<OrderProfile>();
            };

            using var lenientProvider = BuildProvider(services => services.AddMapFlux(Options(strict: false)));
            using var strictProvider = BuildProvider(services => services.AddMapFlux(Options(strict: true)));

            Assert.NotNull(lenientProvider.GetRequiredService<IMapper>());
            Assert.Throws<InvalidOperationException>(() => strictProvider.GetRequiredService<IExistingDestinationMapper>());
        }

        [Fact]
        public void AddMapFlux_StrictValidationWithoutValidateConfiguration_DoesNotValidate()
        {
            using var provider = BuildProvider(services => services.AddMapFlux((MapFluxOptions options) =>
            {
                options.StrictValidation = true;
                options.AddProfile<OrderProfile>();
            }));

            Assert.NotNull(provider.GetRequiredService<IMapper>());
        }

        [Fact]
        public void AddMapFlux_RunsTheOptionsCallbackOnce_AndIgnoresLaterChangesToTheOptions()
        {
            var calls = 0;
            MapFluxOptions? captured = null;
            using var provider = BuildProvider(services => services.AddMapFlux((MapFluxOptions options) =>
            {
                calls++;
                captured = options;
                options.AddProfile<CustomerProfile>();
            }));

            captured!.MaxDepth = 1;
            captured.ValidateConfiguration = true;
            captured.Configure(cfg => cfg.CreateMap<Customer, CustomerSummary>(m => { }));
            var mapper = provider.GetRequiredService<IMapper>();

            Assert.Equal(1, calls);
            Assert.Equal(new Mapper().MaxDepth, ((Mapper)mapper).MaxDepth);
            Assert.Throws<InvalidOperationException>(() => mapper.Map<Customer, CustomerSummary>(new Customer()));
        }

        [Fact]
        public async Task AddMapFlux_ResolvedFromManyThreads_ReturnsOneInstanceThatMaps()
        {
            using var provider = BuildProvider(services => services.AddMapFlux(typeof(CustomerProfile).Assembly));
            using var start = new ManualResetEventSlim(false);

            var tasks = Enumerable.Range(0, 32).Select(i => Task.Run(() =>
            {
                start.Wait();
                var mapper = provider.GetRequiredService<IMapper>();
                var dto = mapper.Map<Customer, CustomerDto>(new Customer { Id = i, Name = "n" + i });
                Assert.Equal(i, dto.Id);
                return mapper;
            })).ToArray();

            start.Set();
            var mappers = await Task.WhenAll(tasks);

            Assert.Single(mappers.Distinct());
        }

        [Fact]
        public void AddMapFlux_WithNullArguments_ThrowsArgumentNullException()
        {
            IServiceCollection services = new ServiceCollection();
            IServiceCollection? none = null;

            Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
                none!.AddMapFlux(cfg => cfg.CreateMap<Customer, CustomerDto>(m => { }))).ParamName);
            Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
                none!.AddMapFlux(typeof(CustomerProfile).Assembly)).ParamName);
            Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
                none!.AddMapFlux((MapFluxOptions o) => { })).ParamName);
            Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
                services.AddMapFlux((Action<IMapperConfigurationExpression>)null!)).ParamName);
            Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
                services.AddMapFlux((Action<MapFluxOptions>)null!)).ParamName);
            Assert.Equal("assemblies", Assert.Throws<ArgumentNullException>(() =>
                services.AddMapFlux((Assembly[])null!)).ParamName);
            Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
                new MapFluxOptions().Configure(null!)).ParamName);
            Assert.Empty(services);
        }
    }
}
