using Microsoft.OpenApi.Models;
using UnderstandingEventsourcingExample.Cart;
using UnderstandingEventsourcingExample.Loyalty;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Understanding Eventsourcing", Description = "Implementation example based on the book...", Version = "v1" });
});

builder.Services.AddCartModule(builder.Configuration);
builder.Services.AddLoyaltyModule(builder.Configuration);

var app = builder.Build();

app.UseCartModule();
app.UseLoyaltyModule();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.RegisterCartModuleEndpoints();
app.RegisterLoyaltyModuleEndpoints();

app.Run();

