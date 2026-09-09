namespace Backend_API.Services
{
    public class RawTelemetryBatchBuffer
    {
        private const int BatchSize = 10;

        public string Unit { get; private set; } = string.Empty;

        private double[] _currentBatch = new double[BatchSize];
        private int _currentBatchCount;

        private double[][] _historicalBatches = Array.Empty<double[]>();

        public void AddTelemetryReading(double value, string unit)
        {
            Unit = unit;
            _currentBatch[_currentBatchCount] = value;
            _currentBatchCount++;

            if (_currentBatchCount == BatchSize)
            {
                SealBatch();
            }
        }

        private void SealBatch()
        {
            Array.Resize(ref _historicalBatches, _historicalBatches.Length + 1);
            _historicalBatches[^1] = _currentBatch;

            _currentBatch = new double[BatchSize];
            _currentBatchCount = 0;
        }

        public double[][] GetHistoricalBatches()
        {
            return _historicalBatches;
        }

        public int SealedBatchCount => _historicalBatches.Length;
    }
}
