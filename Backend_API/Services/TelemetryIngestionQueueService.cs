using PROG7312_POE.Shared.DTOs;
using PROG7312_POE.Shared.Models;

namespace Backend_API.Services
{
    public class TelemetryIngestionQueueService
    {
        private const int MaxProcessLog = 60;

        private readonly Queue<ITelemetryPacketModel> _standardQueue = new();

        private readonly PriorityQueue<ITelemetryPacketModel, (int Priority, long Sequence)> _criticalQueue = new();

        private readonly Queue<PacketProcessedDto> _processedRecently = new();
        private readonly object _lock = new();

        private long criticalSequence;
        private long _processedSequence;
        private long _totalStandardEnqueued;
        private long _totalCriticalEnqueued;

        public void StandardEnqueue(ITelemetryPacketModel telemetryPacket)
        {
            lock (_lock)
            {
                _standardQueue.Enqueue(telemetryPacket);
                _totalStandardEnqueued++;
            }
        }

        public void CriticalEnqueue(ITelemetryPacketModel telemetryPacket, int priority)
        {
            lock (_lock)
            {
                _criticalQueue.Enqueue(telemetryPacket, (priority, criticalSequence++));
                _totalCriticalEnqueued++;
            }
        }

        public bool TryDequeueNext(out ITelemetryPacketModel? telemetryPacket, out bool wasCritical)
        {
            lock (_lock)
            {
                if (_criticalQueue.TryDequeue(out telemetryPacket, out _))
                {
                    wasCritical = true;
                    LogProcessed(telemetryPacket, true);
                    return true;
                }

                if (_standardQueue.TryDequeue(out telemetryPacket))
                {
                    wasCritical = false;
                    LogProcessed(telemetryPacket, false);
                    return true;
                }
            }

            telemetryPacket = null;
            wasCritical = false;
            return false;
        }

        private void LogProcessed(ITelemetryPacketModel telemetryPacket, bool wasCritical)
        {
            _processedRecently.Enqueue(new PacketProcessedDto
            {
                Sequence = _processedSequence++,
                DeviceMacAddress = telemetryPacket.DeviceMacAddress,
                MetricName = telemetryPacket.MetricName,
                WasCritical = wasCritical,
                ProcessedAt = DateTime.UtcNow
            });

            while (_processedRecently.Count > MaxProcessLog)
            {
                _processedRecently.Dequeue();
            }
        }

        public StatsQueueDto GetStats()
        {
            lock (_lock)
            {
                return new StatsQueueDto
                {
                    DepthOfStandardQueue = _standardQueue.Count,
                    DepthOfCriticalQueue = _criticalQueue.Count,
                    TotalStandardEnqueued = _totalStandardEnqueued,
                    TotalCriticalEnqueued = _totalCriticalEnqueued,
                    TotalProcessed = _processedSequence,
                    ProcessedRecently = _processedRecently.OrderBy(process => process.Sequence).ToList()
                };
            }
        }
    }
}
