
using Backend.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// Shared student list
var students = new List<Student>
{
    new Student { Id = 1, Name = "Arun", Age = 21, Course = "Full Stack" },
    new Student { Id = 2, Name = "Kumar", Age = 22, Course = "Angular" },
    new Student { Id = 3, Name = "Ravi", Age = 20, Course = "C#" }
};

// OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Hello endpoint
app.MapGet("/hello", () =>
{
    return "Hello Student!";
});

// Get all students
app.MapGet("/students", () =>
{
    return students;
});

// Get one student by ID
app.MapGet("/students/{id}", (int id) =>
{
    var student = students.FirstOrDefault(s => s.Id == id);

    if (student == null)
    {
        return Results.NotFound("Student not found");
    }

    return Results.Ok(student);
});

// Add a student
app.MapPost("/students", (Student student) =>
{
    students.Add(student);

    return Results.Ok(student);
});

// Default Weather Forecast endpoint
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}
