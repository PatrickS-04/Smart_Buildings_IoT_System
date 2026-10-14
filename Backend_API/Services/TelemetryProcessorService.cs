namespace Backend_API.Services
{
    public class TelemetryProcessorService : BackgroundService
    {
        private readonly TelemetryIngestionQueueService _queueIngestion;
        private readonly TelemetryPacketService _telemetryPacketService;

        public TelemetryProcessorService(TelemetryIngestionQueueService ingestionQueueService, TelemetryPacketService telemetryPacketService)
        {
            _queueIngestion = ingestionQueueService;
            _telemetryPacketService = telemetryPacketService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var processedThisPass = 0;

                while (processedThisPass < 200 && _queueIngestion.TryDequeueNext(out var telemetryPacket, out var wasCritical))
                {
                    _telemetryPacketService.AddTelemetryPacket(telemetryPacket!);
                    
                    if (wasCritical)
                    {
                        Console.WriteLine($"[CRITICAL] {telemetryPacket!.DeviceMacAddress} {telemetryPacket.MetricName} = {telemetryPacket.ValueAsAnObject}");
                    }

                    processedThisPass++;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            }
        }
    }
}
