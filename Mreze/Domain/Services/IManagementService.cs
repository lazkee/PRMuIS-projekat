using Domain.Enums;

namespace Domain.Services
{
    public interface IManagementService
    {
        void TakeOrReserveATable(int clientId, ClientType clientType);
    }
}
