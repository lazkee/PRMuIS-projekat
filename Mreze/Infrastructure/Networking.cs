using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Networking
{
    public class ClientDirectory : IClientDirectory
    {
        private readonly ConcurrentDictionary<int, ClientInfo> _clients
            = new ConcurrentDictionary<int, ClientInfo>();

        public void Register(ClientInfo client)
            => _clients[client.Id] = client;

        public bool Unregister(int clientId)
            => _clients.TryRemove(clientId, out _);

        public ClientInfo GetById(int clientId)
            => _clients.TryGetValue(clientId, out var info) ? info : null;

        public IEnumerable<ClientInfo> GetByType(ClientType type)
            => _clients.Values.Where(c => c.Type == type);
    }
}