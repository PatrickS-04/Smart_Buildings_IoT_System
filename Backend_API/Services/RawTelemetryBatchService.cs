using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class RawTelemetryBatchService
    {
        private readonly Dictionary<string, RawTelemetryBatchBuffer> _telemetryBatchBuffers = new();
        private readonly TelemetryPacketService _telemetryPacketService;

        public RawTelemetryBatchService(TelemetryPacketService telemetryPacketService)
        {
            _telemetryPacketService = telemetryPacketService;
        }

        private static string Key(string deviceMacAddress, string metricName)
        {
            return $"{deviceMacAddress}::{metricName}";
        }

        public void BufferTelemetryReading(string deviceMacAddress, string metricName, string unit, double value)
        {
            var key = Key(deviceMacAddress, metricName);
            if (!_telemetryBatchBuffers.TryGetValue(key, out var buffer))
            {
                buffer = new RawTelemetryBatchBuffer();
                _telemetryBatchBuffers[key] = buffer;
            }

            buffer.AddTelemetryReading(value, unit);
        }

        public int SendToTelemetryPacketService(string deviceMacAddress, string metricName)
        {
            var key = Key(deviceMacAddress, metricName);
            if (!_telemetryBatchBuffers.TryGetValue(key, out var buffer))
            {
                return 0;
            }

            var sendCount = 0;

            foreach (var telemetryBatch in buffer.GetHistoricalBatches())
            {
                foreach (var rawValue in telemetryBatch)
                {
                    var telemetryPacket = new TelemetryPacketModel<float>(deviceMacAddress, metricName, (float)rawValue, buffer.Unit);
                    _telemetryPacketService.AddTelemetryPacket(telemetryPacket);
                    sendCount++;
                }
            }
            return sendCount;
        }

        public int GetSealedBatchCount(string deviceMacAddress, string metricName)
        {
            return _telemetryBatchBuffers.TryGetValue(Key(deviceMacAddress, metricName), out var buffer) ? buffer.SealedBatchCount : 0;
        }
    }
}
