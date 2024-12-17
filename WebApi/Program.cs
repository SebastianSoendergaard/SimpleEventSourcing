using EventSourcing.EventStore;
using EventSourcing.EventStore.PostgreSql;
using EventSourcing.Projections;
using WebApi.User;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//InmemoryEventStore eventStore = new();
PostgreSqlEventStore eventStore = new("Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_event_sourcing;", "event_store");
ProjectionManager projectionManager = new(eventStore);
UserProjector userProjector = new();
UserNameProjector userNameProjector = new();
projectionManager.RegisterLiveProjector(userProjector);
projectionManager.RegisterLiveProjector(userNameProjector);

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
