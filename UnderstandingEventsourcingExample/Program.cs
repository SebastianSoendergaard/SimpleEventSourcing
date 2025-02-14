using Basses.SimpleEventStore.PostgreSql;
using Microsoft.OpenApi.Models;
using UnderstandingEventsourcingExample.Cart;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Understanding Eventsourcing", Description = "Implementation example based on the book...", Version = "v1" });
});

PostgreSqlEventStore eventStore = new(
    "Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_event_sourcing;",
    "understanding_eventsourcing",
    "event_store");
builder.Services.AddSingleton<IEventStore>(eventStore);

builder.Services.AddCartModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.RegisterCartModuleEndpoints();

app.Run();

