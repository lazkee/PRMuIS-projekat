using System.Collections.Generic;
using Domain.Models;

namespace Domain.Services
{
    public interface IReadService
    {
        IEnumerable<Table> GetAllTables();
    }
}
