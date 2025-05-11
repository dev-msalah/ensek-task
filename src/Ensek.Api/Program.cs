using Ensek.Api.Endpoints;
using Ensek.Api.Extensions;
using Ensek.Core.Configuration;
using Ensek.Core.Interfaces;
using Ensek.Infrastructure;
using Ensek.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var clientAppUrl = builder.Configuration.GetValue<string>("ClientApp:Url");

builder.Services.AddControllers();
builder.Services
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ENSEK Meter Reading API",
            Version = "v1.0",
            Description = "ENSEK Meter Reading API",
            Contact = new OpenApiContact { Name = "ENSEK", Email = "contact@ensek.com" }
        });
        c.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "ENSEK Meter Reading API",
            Version = "v2.0",
            Description = "ENSEK Meter Reading API V2",
            Contact = new OpenApiContact { Name = "ENSEK", Email = "contact@ensek.com" }
        });
    })
    .AddApiVersionService()
    .AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton<IMeterReadingValidator, MeterReadingValidator>();
builder.Services.AddScoped<IMeterReadingService, MeterReadingService>();
builder.Services.Configure<MeterReadingConfig>(builder.Configuration.GetSection("MeterReadingConfig"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins(clientAppUrl)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

EndpointConventionBuilderExtensions.InitializeApiVersionSet(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ENSEK Meter Reading API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "ENSEK Meter Reading API V2");
        c.RoutePrefix = "docs";
    });
}

app.UseDatabaseMigrationAndSeeding();
app.UseHttpsRedirection();
app.UseRouting();
app.MeterReadingModule();
app.UseCors("AllowReactApp");

app.Run();