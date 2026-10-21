using System.Text.Json.Serialization;
using EmployeeServiceDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.API.Middleware;
using ServiceDesk.Application;
using ServiceDesk.Infrastructure;
using ServiceDesk.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Angular dev server
builder.Services.AddCors(options => options.AddPolicy("Angular", policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Convenience for local development: create / update the database on startup.
    if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

app.UseHttpsRedirection();
app.UseCors("Angular");

// TODO(Member 1): app.UseAuthentication(); once JWT is wired up.
app.UseAuthorization();

app.MapControllers();
app.Run();

public partial class Program { } // for integration tests
   