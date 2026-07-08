using Jx3.Common.Utils;

namespace Jx3.Common.Service;

public static class ServiceRegistry
{
    private static readonly Dictionary<uint, Func<byte[], Task<byte[]>>> _handlers = new();
    private static readonly Dictionary<string, object> _services = new();

    public static void RegisterHandler(uint msgId, Func<byte[], Task<byte[]>> handler)
    {
        _handlers[msgId] = handler;
    }

    public static void RegisterService(string name, object service)
    {
        _services[name] = service;
        Logger.Info("Registry", $"Service registered: {name}");
    }

    public static T? GetService<T>(string name) where T : class
    {
        return _services.GetValueOrDefault(name) as T;
    }

    public static async Task<byte[]?> RouteAsync(uint msgId, byte[] body)
    {
        if (_handlers.TryGetValue(msgId, out var handler))
        {
            return await handler(body);
        }
        Logger.Warn("Registry", $"No handler for MsgId: {msgId}");
        return null;
    }
}
