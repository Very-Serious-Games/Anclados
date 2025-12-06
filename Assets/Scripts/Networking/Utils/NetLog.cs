using UnityEngine;

/// <summary>
/// Centralized network logging utility with categories and verbosity levels
/// </summary>
public static class NetLog
{
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug,
        Verbose
    }

    public static LogLevel CurrentLogLevel = LogLevel.Info;

    // -------- Client Logging -------- //

    public static void Client(string message, LogLevel level = LogLevel.Info)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix("CLIENT", level);
        Log(prefix + message, level);
    }

    public static void ClientError(string message)
    {
        Client(message, LogLevel.Error);
    }

    public static void ClientWarning(string message)
    {
        Client(message, LogLevel.Warning);
    }

    // -------- Server Logging -------- //

    public static void Server(string message, LogLevel level = LogLevel.Info)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix("SERVER", level);
        Log(prefix + message, level);
    }

    public static void ServerError(string message)
    {
        Server(message, LogLevel.Error);
    }

    public static void ServerWarning(string message)
    {
        Server(message, LogLevel.Warning);
    }

    // -------- Transport Logging -------- //

    public static void Transport(string transportType, string message, LogLevel level = LogLevel.Info)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix($"{transportType} TRANSPORT", level);
        Log(prefix + message, level);
    }

    public static void TransportError(string transportType, string message)
    {
        Transport(transportType, message, LogLevel.Error);
    }

    // -------- General Network Logging -------- //

    public static void Network(string message, LogLevel level = LogLevel.Info)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix("NETWORK", level);
        Log(prefix + message, level);
    }

    public static void NetworkError(string message)
    {
        Network(message, LogLevel.Error);
    }

    public static void NetworkWarning(string message)
    {
        Network(message, LogLevel.Warning);
    }

    // -------- Message Logging -------- //

    public static void Message(string direction, string messageType, LogLevel level = LogLevel.Verbose)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix($"MSG {direction}", level);
        Log($"{prefix}{messageType}", level);
    }

    public static void MessageSent(string messageType)
    {
        Message("SENT", messageType);
    }

    public static void MessageReceived(string messageType)
    {
        Message("RECV", messageType);
    }

    // -------- Heartbeat Logging -------- //

    public static void Heartbeat(string message, LogLevel level = LogLevel.Debug)
    {
        if (level > CurrentLogLevel) return;
        
        string prefix = GetPrefix("HEARTBEAT", level);
        Log(prefix + message, level);
    }

    // -------- Helper Methods -------- //

    private static string GetPrefix(string category, LogLevel level)
    {
        string timestamp = Time.time.ToString("F2");
        string levelStr = level == LogLevel.Info ? "" : $" [{level.ToString().ToUpper()}]";
        return $"[{timestamp}] [{category}]{levelStr} ";
    }

    private static void Log(string message, LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Error:
                Debug.LogError(message);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message);
                break;
            default:
                Debug.Log(message);
                break;
        }
    }

    // -------- Configuration -------- //

    public static void SetLogLevel(LogLevel level)
    {
        CurrentLogLevel = level;
        Debug.Log($"[NetLog] Log level set to: {level}");
    }

    public static void EnableVerboseLogging()
    {
        SetLogLevel(LogLevel.Verbose);
    }

    public static void EnableDebugLogging()
    {
        SetLogLevel(LogLevel.Debug);
    }

    public static void DisableDebugLogging()
    {
        SetLogLevel(LogLevel.Info);
    }
}
