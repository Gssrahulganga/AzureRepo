using System.Reflection;
using Microsoft.OpenApi.Models;
using Azure.Messaging.ServiceBus;
using Demo.Miroservices.Azure.Order.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Service Bus registration...
var sbConnection = builder.Configuration.GetValue<string>("ServiceBus:ConnectionString");
if (string.IsNullOrWhiteSpace(sbConnection))
{
    throw new InvalidOperationException("ServiceBus:ConnectionString is not configured. Set it in appsettings.json or secrets.");
}
builder.Services.AddSingleton(_ => new ServiceBusClient(sbConnection));
builder.Services.AddSingleton<IOrderPublisher, ServiceBusOrderPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
