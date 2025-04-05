using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.PostgreSql;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Reactions;
using WebApi.User;

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_event_sourcing;";

SimpleObjectDbConfiguration config = new();
config.RegisterDataType<PersistedUserProjectorState>(i => i.Name);
SimplePostgreSqlObjectDb.CreateIfNotExist(connectionString, config);
builder.Services.AddSingleton<ISimpleObjectDb>(x => new SimplePostgreSqlObjectDb(connectionString, config));

//builder.Services.AddSingleton<IEventStore>(x => new InMemoryEventStore());
//builder.Services.AddSingleton<IEventStore>(x => new FileEventStore(@"c:/temp/eventstore"));
builder.Services.AddSingleton<IEventStore>(x => new PostgreSqlEventStore(connectionString, "web_api", $"event_store"));
//builder.Services.AddSingleton<IProjectorStateStore>(x => new InMemoryProjectorStateStore());
//builder.Services.AddSingleton<IProjectorStateStore>(x => new FileProjectorStateStore(@"c:/temp/eventstore/state"));
builder.Services.AddSingleton<IProjectorStateStore>(x => new PostgreSqlProjectorStateStore(connectionString, "web_api", $"event_store_projector_state"));
builder.Services.AddSingleton<IReactorStateStore>(x => new PostgreSqlReactorStateStore(connectionString, "web_api", $"event_store_reactor_state"));
builder.Services.AddSingleton<ProjectionManager>();

builder.Services.AddSingleton<UserProjector>();
builder.Services.AddSingleton<UserNameProjector>();
builder.Services.AddScoped<PersistedUserProjector>();

builder.Services.AddScoped<UserRepository>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var projectionManager = app.Services.GetRequiredService<ProjectionManager>();
projectionManager.RegisterSynchronousProjector<UserProjector>();
projectionManager.RegisterSynchronousProjector<UserNameProjector>();
projectionManager.RegisterSynchronousProjector<PersistedUserProjector>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//projectionManager.Start();
app.Run();
//projectionManager.Stop();
