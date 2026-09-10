using Backend_API.Endpoints;
using Backend_API.Services;
using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SensorRegistrationService>();
builder.Services.AddSingleton<TelemetryPacketService>();
builder.Services.AddSingleton<BuildingConfigurationService>();
builder.Services.AddSingleton<RawTelemetryBatchService>();
builder.Services.AddSingleton<AttachmentsForSensorsService>();
builder.Services.AddHttpClient("TimerApi", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://localhost:7194/");
});

builder.Services.AddHostedService<SimulatedTelemetryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7070",
                "http://localhost:5055")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");

app.MapSensorEndpoints();
app.MapTelemetryPacketEndpoints();
app.MapTelemetryBatchEndpoints();
app.MapSensorAttachmentEndpoints();

app.Run();
