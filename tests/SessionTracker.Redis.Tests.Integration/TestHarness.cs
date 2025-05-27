// namespace SessionTracker.Redis.Tests.Integration;
//
// public class TestHarness : IDisposable
// {
//     private readonly List<TestIntegrationContext> _testContexts = new();
//     
//     public IRepository<TestEntity> CreateRepository()
//     {
//         var ctx = new TestIntegrationContext(_dbContextOptions, _config, _timeProvider);
//         _testContexts.Add(ctx);
//         var repo = new Repository<TestEntity>(ctx, _specificationEvaluator, _mapper);
//         return repo;
//     }
//     
//     public IReadOnlyRepository<TestEntity> CreateReadOnlyRepository()
//     {
//         var ctx = new TestIntegrationContext(_dbContextOptions, _config, _timeProvider);
//         _testContexts.Add(ctx);
//         var repo = new ReadOnlyRepository<TestEntity>(ctx, _specificationEvaluator, _mapper);
//         return repo;
//     }
//
//     private readonly IOptions<DataExplorerEfCoreConfiguration> _config;
//     private readonly DataExplorerTimeProvider _timeProvider;
//     private readonly DbContextOptions<TestIntegrationContext> _dbContextOptions;
//     private readonly IEfSpecificationEvaluator _specificationEvaluator;
//     private readonly IMapper _mapper;
//     
//     private readonly PostgreSqlContainer _container;
//     
//     public RepositoryFixture()
//     {
//         _config = Options.Create(new DataExplorerEfCoreConfiguration(new MicrosoftRegistrator(new ServiceCollection())));
//         
//         _timeProvider = new DataExplorerTimeProvider.StaticDataExplorerTimeProvider();
//         
//         _specificationEvaluator = new SpecificationEvaluator(true);
//
//         _mapper = Mock.Of<IMapper>();
//
//         var port = Random.Shared.Next(5000, 10000);
//         
//         _container = new PostgreSqlBuilder()
//             .WithDatabase("test")
//             .WithPassword("test")
//             .WithUsername("test")
//             .WithImage("postgres:17.5-bookworm")
//             .WithPortBinding(port, port)
//             .Build();
//
//         Task.Delay(2000).Wait();
//         
//         _container.StartAsync().Wait();
//         
//         var optionsBuilder = new DbContextOptionsBuilder<TestIntegrationContext>();
//
// #if NET9_0_OR_GREATER
//         optionsBuilder.ConfigureWarnings(x => x.Ignore(RelationalEventId.PendingModelChangesWarning));
// #endif
//         
//         optionsBuilder.UseNpgsql(_container.GetConnectionString());
//         _dbContextOptions = optionsBuilder.Options;
//         
//         var initCtx = new TestIntegrationContext(_dbContextOptions, _config, _timeProvider);
//         initCtx.Database.Migrate();
//         initCtx.Dispose();
//     }
//
//     public void Dispose()
//     {
//         var last = _testContexts.Last();
//         
//         foreach (var ctx in _testContexts)
//         {
//             if (!ReferenceEquals(ctx,last))
//             {
//                 ctx.Dispose();
//             }
//         }
//
//         last.Database.EnsureDeleted();
//         last.Dispose();
//
//         _container.StopAsync().Wait();
//         _container.DisposeAsync().AsTask().Wait();
//     }
// }