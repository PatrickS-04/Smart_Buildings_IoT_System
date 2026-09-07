using System;
using System.Collections.Generic;
using System.Text;

namespace PROG7312_POE.Shared.Models;

public readonly struct PowerReading : IComparable<PowerReading>
{
    public double Kilowatts { get; }
    public string SourceDescription { get; }

    public PowerReading(double kilowatts, string sourceDescription = "")
    {
        Kilowatts = kilowatts;
        SourceDescription = sourceDescription;
    }

    public static PowerReading operator +(PowerReading a, PowerReading b) =>
        new(a.Kilowatts + b.Kilowatts, CombineSources(a, b));

    public static PowerReading operator -(PowerReading a, PowerReading b) =>
        new(a.Kilowatts - b.Kilowatts, CombineSources(a, b));

    public static bool operator >(PowerReading a, PowerReading b) => a.Kilowatts > b.Kilowatts;
    public static bool operator <(PowerReading a, PowerReading b) => a.Kilowatts < b.Kilowatts;
    public static bool operator >=(PowerReading a, PowerReading b) => a.Kilowatts >= b.Kilowatts;
    public static bool operator <=(PowerReading a, PowerReading b) => a.Kilowatts <= b.Kilowatts;

    public int CompareTo(PowerReading other) => Kilowatts.CompareTo(other.Kilowatts);

    private static string CombineSources(PowerReading a, PowerReading b) =>
        string.IsNullOrEmpty(a.SourceDescription) || string.IsNullOrEmpty(b.SourceDescription)
            ? $"{a.SourceDescription}{b.SourceDescription}"
            : $"{a.SourceDescription} + {b.SourceDescription}";

    public override string ToString()
    {
        return $"{Kilowatts:0.0} kW";
    }

}