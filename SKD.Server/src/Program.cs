#nullable disable

var builder = WebApplication.CreateBuilder(args);

var appSettings = new AppSettings(builder.Configuration);

// Confiture services
builder.Services
    .AddDbContext<SkdContext>(options => {
        options.UseSqlServer(appSettings.DefaultConnectionString, sqlOptions => {
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
    })
    .AddCors(options => options.AddDefaultPolicy(
       policy => policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod()
    ))    
    .AddApplicationInsightsTelemetry();

// Domain services
builder.Services
    .AddScoped<KitService>(sp =>
        new KitService(sp.GetRequiredService<SkdContext>(), currentDate: DateTime.Now))
    .AddScoped<KitSnapshotService>()
    .AddScoped<PcvService>()
    .AddScoped<ComponentService>()
    .AddScoped<DCWSResponseService>()
    .AddScoped<ProductionStationService>()
    .AddScoped<ComponentSerialService>(sp =>
        new ComponentSerialService(sp.GetRequiredService<SkdContext>()))
    .AddScoped<ShipmentService>()
    .AddScoped<BomService>()
    .AddScoped<PlantService>()
    .AddScoped<LotPartService>()
    .AddScoped<HandlingUnitService>()
    .AddScoped<QueryService>().AddSingleton<DcwsService>(sp => new DcwsService(appSettings.DcwsServiceAddress))
    .AddScoped<PartnerStatusBuilder>()
    .AddScoped<KitVinAckBuilder>()
    .AddScoped<VerifySerialService>()
    .AddScoped<DevMutation>(sp => new DevMutation(builder.Environment.IsDevelopment()));

// Hotchocolate Graphql Server 
builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<SkdContext>()
    .AddQueryType<Query>()
        .AddTypeExtension<ProjectionQueries>()
    .AddMutationType<Mutation>()
        .AddTypeExtension<DevMutation>()
    .AddType<VehicleType>()
    .AddType<SerialCaptureVehicleDTOType>()
    .AddType<ComponentSerialDtoType>()
    .AddType<VinFileInputType>()
    .AddType<VinFileType>()
    .AddType<VehicleTimelineDTOType>()
    .AddType<PcvType>()
    .AddType<VehicleComponentType>()
    .AddType<KitListItemDtoType>()
    .AddType<KitVinType>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddInMemorySubscriptions()
    .ModifyRequestOptions(opt => {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
        if (appSettings.ExecutionTimeoutSeconds > 0) {
            opt.ExecutionTimeout = TimeSpan.FromSeconds(appSettings.ExecutionTimeoutSeconds);
        }
    })
    .AllowIntrospection(appSettings.AllowGraphqlIntrospection);

// build
var app = builder.Build();

app.UseCors();
app.MapGraphQL();
app.Run();
