using System.Collections.Concurrent;

namespace ClubPlatform.Api.Services.Chat;

/// <summary>
/// Отслеживает активные SignalR-подключения пользователей к клубам.
/// </summary>
public class ClubConnectionTracker
{
    private readonly ConcurrentDictionary<
        (Guid ClubId, Guid UserId),
        ConcurrentDictionary<string, byte>> _connections = new();

    /// <summary>
    /// Регистрирует подключение пользователя к клубу.
    /// </summary>
    public void Add(Guid clubId, Guid userId, string connectionId)
    {
        var connections = _connections.GetOrAdd(
            (clubId, userId),
            _ => new ConcurrentDictionary<string, byte>());

        connections.TryAdd(connectionId, 0);
    }

    /// <summary>
    /// Удаляет подключение пользователя из конкретного клуба.
    /// </summary>
    public void Remove(Guid clubId, Guid userId, string connectionId)
    {
        var key = (clubId, userId);

        if (!_connections.TryGetValue(key, out var connections))
            return;

        connections.TryRemove(connectionId, out _);

        if (connections.IsEmpty)
            _connections.TryRemove(key, out _);
    }

    /// <summary>
    /// Удаляет подключение из отслеживания во всех клубах.
    /// Используется при отключении SignalR-соединения.
    /// </summary>
    public void RemoveConnectionFromAllClubs(string connectionId)
    {
        foreach (var item in _connections)
        {
            item.Value.TryRemove(connectionId, out _);

            if (item.Value.IsEmpty)
                _connections.TryRemove(item.Key, out _);
        }
    }

    /// <summary>
    /// Возвращает все подключения пользователя к конкретному клубу.
    /// </summary>
    public IReadOnlyCollection<string> GetConnections(
        Guid clubId,
        Guid userId)
    {
        if (!_connections.TryGetValue(
            (clubId, userId),
            out var connections))
        {
            return Array.Empty<string>();
        }

        return connections.Keys.ToArray();
    }
}
