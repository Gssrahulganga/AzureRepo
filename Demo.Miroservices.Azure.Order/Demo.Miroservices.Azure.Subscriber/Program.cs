using Azure.Messaging.ServiceBus;
using Demo.Miroservices.Azure.Subscriber.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register ServiceBus client and the background subscriber
var sbConnection = builder.Configuration.GetValue<string>("ServiceBus:ConnectionString");
if (string.IsNullOrWhiteSpace(sbConnection))
{
    throw new InvalidOperationException("ServiceBus:ConnectionString is not configured. Set it in appsettings.json or secrets.");
}

builder.Services.AddSingleton(_ => new ServiceBusClient(sbConnection));
builder.Services.AddHostedService<ServiceBusOrderSubscriber>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
