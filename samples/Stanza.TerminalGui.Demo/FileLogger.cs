namespace Stanza.TerminalGui.Demo;

public class FileLogger : ILogger, System.IDisposable
{
    private readonly string _path = "stanza_bindings.log";
    private readonly System.Collections.Concurrent.BlockingCollection<string> _queue = new();
    private readonly System.Threading.Tasks.Task _writeTask;

    public FileLogger()
    {
        System.IO.File.WriteAllText(
            _path,
            $"=== Stanza Binding Log Started at {System.DateTime.Now} ===\n"
        );
        _writeTask = System.Threading.Tasks.Task.Run(ProcessQueue);
    }

    public void Log(string message)
    {
        _queue.Add($"[{System.DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    private void ProcessQueue()
    {
        foreach (var msg in _queue.GetConsumingEnumerable())
        {
            System.IO.File.AppendAllText(_path, msg + "\n");
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _writeTask.Wait();
        _queue.Dispose();
    }
}
