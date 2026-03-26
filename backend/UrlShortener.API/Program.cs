using UrlShortener.API.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read MongoDB connection string from environment variable
// Set MONGODB_URI on Render, or use MongoDB Atlas free tier
var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI")
               ?? "mongodb://localhost:27017";

builder.Services.AddSingleton(new MongoDbService(mongoUri));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();

public partial class Program { }