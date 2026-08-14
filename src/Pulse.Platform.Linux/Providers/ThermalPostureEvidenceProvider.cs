using System.Globalization;

namespace Pulse.Platform.Linux.Providers;

public sealed class ThermalPostureEvidenceProvider(string thermalRoot = "/sys/class/thermal") : ILinuxEvidenceProvider
{
    public string Id => "linux.performance-thermal";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(thermalRoot))
        {
            return EvidenceResult.Unavailable(Id, "Thermal posture", thermalRoot,
                "The standard Linux thermal class is not available.");
        }

        var readings = new List<(string Name, double Celsius)>();
        foreach (var directory in Directory.EnumerateDirectories(thermalRoot, "thermal_zone*"))
        {
            var temperaturePath = System.IO.Path.Combine(directory, "temp");
            if (!File.Exists(temperaturePath))
            {
                continue;
            }

            var text = await File.ReadAllTextAsync(temperaturePath, cancellationToken);
            if (!double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
            {
                continue;
            }

            var celsius = Math.Abs(raw) > 1000 ? raw / 1000d : raw;
            if (celsius is < -20 or > 200)
            {
                continue;
            }

            var typePath = System.IO.Path.Combine(directory, "type");
            var name = File.Exists(typePath)
                ? (await File.ReadAllTextAsync(typePath, cancellationToken)).Trim()
                : System.IO.Path.GetFileName(directory);
            readings.Add((string.IsNullOrWhiteSpace(name) ? "thermal zone" : name, celsius));
        }

        if (readings.Count == 0)
        {
            return EvidenceResult.Unavailable(Id, "Thermal posture", $"{thermalRoot}/thermal_zone*/temp",
                "No readable thermal-zone temperatures were exposed to Pulse.");
        }

        var hottest = readings.MaxBy(reading => reading.Celsius);
        var state = hottest.Celsius >= 95
            ? EvidenceState.Attention
            : hottest.Celsius >= 85
                ? EvidenceState.Informational
                : EvidenceState.Healthy;
        return new(Id, "Thermal posture", state,
            $"The hottest readable thermal zone is {hottest.Name} at {hottest.Celsius:F0} °C across {readings.Count} zone(s).",
            state == EvidenceState.Attention
                ? "Temperature is high enough to review airflow and active workload. Let intentional heavy work finish and follow the computer manufacturer's thermal guidance; Pulse will not change fan or power settings."
                : "Thermal readings vary by hardware and sensor placement. Pulse records the hottest exposed zone as context and does not control fans, clocks, or power profiles.",
            $"{thermalRoot}/thermal_zone*/temp and type");
    }
}
