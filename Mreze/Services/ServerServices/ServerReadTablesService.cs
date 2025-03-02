using System.Collections.Generic;
using Domain.Models;
using Domain.Repositories.TableRepository;
using Domain.Services;

namespace Services.ServerServices
{
    public class ServerReadTablesService : IReadService
    {
        ITableRepository tables = new TableRepository();

        public IEnumerable<Table> GetAllTables()
        {
            return tables.GetAllTables();
        }
    }
}
