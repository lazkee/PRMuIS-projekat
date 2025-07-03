using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.ManagerRepository
{
    public interface IManagerRepository
    {
        bool GetManagerState(int managerId);
        void SetManagerState(int managerId, bool isBusy);
        void RequestFreeTable(int managerId, int serverPort, int numberOfGuests);
        void RemoveReservation(int reservationId);
        IEnumerable<(int Key, Reservation Table)> GetExpiredReservations();

        bool CheckReservation(int reservationId);
    }
}
