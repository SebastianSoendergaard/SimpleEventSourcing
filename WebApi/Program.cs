using EventSourcing.EventStore;
using EventSourcing.EventStore.PostgreSql;
using EventSourcing.Projections;
using EventSourcing.Projections.InMemory;
using WebApi.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//InmemoryEventStore eventStore = new();

//var directoryPath = @"c:/temp/eventstore";
//var eventStore = new FileEventStore(directoryPath);

var postgresConnectionString = "Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_event_sourcing;";
PostgreSqlEventStore.CreateIfNotExist(postgresConnectionString, "event_store");
PostgreSqlEventStore eventStore = new(postgresConnectionString, "event_store");

var projectorStateStore = new InMemoryProjectorStateStore();

ProjectionManager projectionManager = new(eventStore, projectorStateStore);
projectionManager.RegisterLiveProjector(new UserProjector());
projectionManager.RegisterLiveProjector(new UserNameProjector());

builder.Services.AddSingleton<IEventStore>(eventStore);
builder.Services.AddSingleton(projectionManager);

builder.Services.AddScoped<UserRepository>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
