using System.Collections.Generic;
using Domain.Enums;
using Domain.Models;

namespace Domain.Repositories
{
    public interface IClientDirectory
    {
        void Register(ClientInfo client);
        bool Unregister(int clientId);
        ClientInfo GetById(int clientId);
        IEnumerable<ClientInfo> GetByType(ClientType type);
    }


}

