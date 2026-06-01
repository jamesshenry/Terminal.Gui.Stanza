using CommunityToolkit.Mvvm.ComponentModel;

namespace Terminal.Gui.Stanza.ClusterExplorer;

public partial class ClusterExplorerViewModel : ObservableObject, IDisposable
{
    public const float SampleIntervalMs = 1000f;

    private static readonly (string Name, string Ip)[] Nodes =
    [
        ("Node-Alpha",   "10.0.0.12"),
        ("Node-Beta",    "10.0.0.13"),
        ("Node-Gamma",   "10.0.0.14"),
    ];

    private static readonly Random Rng = new();
    private readonly Queue<float> _cpuHistoryBuffer = [];
    private const int CpuHistoryLength = 32;
    private int _nodeIndex = 0;
    private int _cpuValue = 45;
    private int _ramValue = 72;
    private readonly Timer _timer;

    [ObservableProperty]
    public partial string ClusterName {get;set;} = "Prod-Cluster-01";

    [ObservableProperty]
    public partial string NodeName {get;set;} = "Node-Alpha";

    [ObservableProperty]
    public partial string NodeIp {get;set;} = "10.0.0.12";

    [ObservableProperty]
    public partial string CpuUsageText {get;set;} = "45%";

    [ObservableProperty]
    public partial string RamUsageText {get;set;} = "72%";

    [ObservableProperty]
    public partial float[] CpuHistory {get;set;}= [];

    public ClusterExplorerViewModel()
    {
        SeedCpuHistory();
        _timer = new Timer(_ => Simulate(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void Simulate()
    {
        var cpuDelta = Rng.Next(-8, 9);
        if (cpuDelta == 0)
        {
            cpuDelta = Rng.Next(0, 2) == 0 ? -1 : 1;
        }

        _cpuValue = Math.Clamp(_cpuValue + cpuDelta, 1, 99);
        _ramValue = Math.Clamp(_ramValue + Rng.Next(-3, 4), 1, 99);

        CpuUsageText = $"{_cpuValue}%";
        RamUsageText = $"{_ramValue}%";
        PushCpuHistory(_cpuValue);

        if (Rng.Next(5) == 0)
        {
            _nodeIndex = (_nodeIndex + 1) % Nodes.Length;
            NodeName = Nodes[_nodeIndex].Name;
            NodeIp   = Nodes[_nodeIndex].Ip;
        }
    }

    private void SeedCpuHistory()
    {
        for (var i = 0; i < CpuHistoryLength; i++)
        {
            var wave = MathF.Sin(i / 3f) * 6f;
            var jitter = Rng.Next(-2, 3);
            var seededValue = Math.Clamp(_cpuValue + (int)wave + jitter, 1, 99);
            _cpuHistoryBuffer.Enqueue(seededValue);
        }

        _cpuValue = (int)_cpuHistoryBuffer.Last();
        CpuUsageText = $"{_cpuValue}%";
        CpuHistory = [.. _cpuHistoryBuffer];
    }

    private void PushCpuHistory(float value)
    {
        _cpuHistoryBuffer.Enqueue(value);

        while (_cpuHistoryBuffer.Count > CpuHistoryLength)
        {
            _cpuHistoryBuffer.Dequeue();
        }

        CpuHistory = [.. _cpuHistoryBuffer];
    }

    public void Dispose() => _timer.Dispose();
}
