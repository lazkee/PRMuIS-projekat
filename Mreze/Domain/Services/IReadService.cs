using System.Collections.Generic;
using Domain.Models;

namespace Domain.Services
{
    public interface IReadService
    {
        IEnumerable<Table> GetAllTables();
        int GetFreeTableFor(int numGuests);
        void OccupyTable(int tableNumber, int waiterId);
        void ReleaseTable(int tableNumber);
    }
}
