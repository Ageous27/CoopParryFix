using System.Text;
using BepInEx.Logging;

namespace CoopParryFix;

internal sealed class PluginLog
{
    private readonly ManualLogSource _bepInEx;
    private readonly StreamWriter? _file;
    private readonly object _gate = new();

    internal PluginLog(ManualLogSource bepInEx, string filePath)
    {
        _bepInEx = bepInEx;
        try
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            _file = new StreamWriter(filePath, append: false, new UTF8Encoding(false))
            {
                AutoFlush = true
            };
            WriteLine("Info", $"CoopParryFix log {filePath}");
        }
        catch (Exception ex)
        {
            bepInEx.LogWarning($"CoopParryFix could not open plugin log file: {ex.Message}");
        }
    }

    internal void LogInfo(object data)
    {
        _bepInEx.LogInfo(data);
        WriteLine("Info", data);
    }

    internal void LogWarning(object data)
    {
        _bepInEx.LogWarning(data);
        WriteLine("Warning", data);
    }

    internal void LogError(object data)
    {
        _bepInEx.LogError(data);
        WriteLine("Error", data);
    }

    internal void Close()
    {
        lock (_gate)
        {
            try { _file?.Dispose(); }
            catch { /* shutdown */ }
        }
    }

    private void WriteLine(string level, object data)
    {
        if (_file == null)
            return;
        try
        {
            lock (_gate)
            {
                _file.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-7}] {data}");
            }
        }
        catch
        {
            // Never break combat because the log file failed.
        }
    }
}
