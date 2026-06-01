using CommunityToolkit.Mvvm.ComponentModel;

namespace Terminal.Gui.Stanza.ClusterExplorer;

public partial class ClusterExplorerViewModel : ObservableObject, IDisposable
{
    private static readonly (string Name, string Ip)[] Nodes =
    [
        ("Node-Alpha",   "10.0.0.12"),
        ("Node-Beta",    "10.0.0.13"),
        ("Node-Gamma",   "10.0.0.14"),
    ];

    private static readonly Random Rng = new();
    private int _nodeIndex = 0;
    private int _cpuValue = 45;
    private int _ramValue = 72;
    private readonly Timer _timer;

    [ObservableProperty]
    private string _clusterName = "Prod-Cluster-01";

    [ObservableProperty]
    private string _nodeName = "Node-Alpha";

    [ObservableProperty]
    private string _nodeIp = "10.0.0.12";

    [ObservableProperty]
    private string _cpuUsageText = "45%";

    [ObservableProperty]
    private string _ramUsageText = "72%";

    public ClusterExplorerViewModel()
    {
        _timer = new Timer(_ => Simulate(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void Simulate()
    {
        _cpuValue = Math.Clamp(_cpuValue + Rng.Next(-5, 6), 1, 99);
        _ramValue = Math.Clamp(_ramValue + Rng.Next(-3, 4), 1, 99);

        CpuUsageText = $"{_cpuValue}%";
        RamUsageText = $"{_ramValue}%";

        if (Rng.Next(5) == 0)
        {
            _nodeIndex = (_nodeIndex + 1) % Nodes.Length;
            NodeName = Nodes[_nodeIndex].Name;
            NodeIp   = Nodes[_nodeIndex].Ip;
        }
    }

    public void Dispose() => _timer.Dispose();
}
