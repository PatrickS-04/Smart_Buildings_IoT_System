namespace Backend_API.Services
{
    public static class ProfileOfSensorSignal
    {
        public const int CycleMinutes = 15;

        private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private record Profile(double Mid, double Amp, double Noise, double Min, double Max, string Unit);

        private static readonly Dictionary<string, Profile> Profiles = new()
        {
            ["Temperature"] = new(22, 4.5, 0.25, 15, 32, "°C"),
            ["Humidity"] = new(45, 17, 0.6, 25, 70, "%"),
            ["CO2 (Carbon Dioxide)"] = new(700, 400, 20, 400, 1400, "ppm"),
            ["Air Quality (VOCs)"] = new(60, 50, 3, 0, 150, "index"),
            ["Light Level (Lux)"] = new(500, 350, 15, 100, 900, "lux"),
            ["Vibration"] = new(0.1, 0.07, 0.004, 0, 0.3, "g"),
            ["Pipe Pressure"] = new(4.0, 1.6, 0.04, 2, 6, "bar"),
            ["Power Meters"] = new(10, 10, 0.4, 0.1, 25, "kW"),
            ["Energy Sub-meters"] = new(10, 10, 0.4, 0.1, 25, "kW"),
            ["Utility Meters"] = new(10, 10, 0.4, 0.1, 25, "kW"),
            ["People Counting"] = new(30, 35, 1.5, 0, 80, "count")
        };

        public static bool IsCyclic(string sensorType) => Profiles.ContainsKey(sensorType);

        public static string UnitOf(string sensorType) => Profiles[sensorType].Unit;

        public static double ValueAt(string sensorType, string deviceMacAddress, DateTime utc, Random random)
        {
            var profile = Profiles[sensorType];
            var hash = StableHash(deviceMacAddress + sensorType);

            var strength = 0.95 + (hash % 100) / 100.0 * 0.13;
            var offset = ((hash / 100) % 30) / 100.0;

            var phase = (utc - Epoch).TotalSeconds / (CycleMinutes * 60.0) + offset;
            var cycle = profile.Mid + profile.Amp * strength * Math.Sin(2 * Math.PI * phase);
            var noise = (random.NextDouble() * 2 - 1) * profile.Noise;

            return Math.Clamp(cycle + noise, profile.Min, profile.Max);
        }

        private static int StableHash(string text)
        {
            var hash = 17;

            foreach (var character in text)
            {
                hash = unchecked(hash * 31 + character);
            }

            return Math.Abs(hash == int.MinValue ? 0 : hash);
        }
    }
}
