﻿using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.PostgreSql;
using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Projections.Files;
using WebApi.User;

var builder = WebApplication.CreateBuilder(args);


SimpleObjectDbConfiguration config = new();
config.RegisterDataType<PersistedUserProjectorState>(i => i.Id);
var projectionPostgresConnectionString = @"Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_object_db;";
SimplePostgreSqlObjectDb.CreateIfNotExist(projectionPostgresConnectionString, config);
ISimpleObjectDb projectionDb = new SimplePostgreSqlObjectDb(projectionPostgresConnectionString, config);

// Add services to the container.
//InmemoryEventStore eventStore = new();

//var directoryPath = @"c:/temp/eventstore";
//var eventStore = new FileEventStore(directoryPath);

var postgresConnectionString = "Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_event_sourcing;";
var eventStore = new PostgreSqlEventStore(postgresConnectionString, "web_api", $"event_store");

var projectorStateStore = new FileProjectorStateStore(@"c:/temp/eventstore/state");

ProjectionManager projectionManager = new(eventStore, projectorStateStore, builder.Build().Services);
projectionManager.RegisterSynchronousProjector(new UserProjector());
projectionManager.RegisterAsynchronousProjector(new UserNameProjector());
projectionManager.RegisterSynchronousProjector(new PersistedUserProjector(projectionDb));

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

projectionManager.Start();
app.Run();
projectionManager.Stop();
