using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.TableRepository
{
    public interface ITableRepository
    {
        IEnumerable<Table> GetAllTables();
        Table GetByID(int id);
        void updateRepository(Table t);
        void clearTable(int n);

    }
}
