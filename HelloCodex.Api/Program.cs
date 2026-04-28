using HelloCodex.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("QuestionnairesDatabase")
    ?? throw new InvalidOperationException("Missing QuestionnairesDatabase connection string.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/ping", () => Results.Text("pong", "text/plain")).WithName("Ping");

app.Run();
