using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.TableRepository
{
    public class TableRepository : ITableRepository
    {
        private static List<Table> tables = new List<Table>();

        public TableRepository()
        {

            for (int i = 1; i < 16; ++i)
            {
                tables.Add(new Table(i, 0, TableState.FREE, new List<Order>()));
            }
            //nmg da stavim nikako drugacije da ih bude odredjen broj (ili ne znam)
        }

        public IEnumerable<Table> GetAllTables()
        {
            IEnumerable<Table> _tables = tables;
            return _tables;
        }

    }
}
