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
builder.Services.AddSingleton<SensorDashboardAggregationService>();
builder.Services.AddSingleton<TelemetryIngestionQueueService>();
builder.Services.AddSingleton<DeviceStateService>();
builder.Services.AddSingleton<TelemetryHistoryService>();
builder.Services.AddSingleton<AlertStateService>();
builder.Services.AddSingleton<CommandService>();

builder.Services.AddHttpClient("TimerApi", httpClient =>
{
    var selfAddress = builder.Environment.IsDevelopment()
        ? "https://localhost:7194/"
        : "http://localhost:8080/";

    httpClient.BaseAddress = new Uri(selfAddress);
});

builder.Services.AddHostedService<SimulatedTelemetryService>();
builder.Services.AddHostedService<TelemetryProcessorService>();

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowClient");

app.MapSensorEndpoints();
app.MapTelemetryPacketEndpoints();
app.MapTelemetryBatchEndpoints();
app.MapSensorAttachmentEndpoints();
app.MapSensorDashboardEndpoints();
app.MapStreamEndpoints();
app.MapCommandEndpoints();

app.Run();
