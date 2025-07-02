using System;
using System.Collections.Generic;
using Domain.Models;

namespace Domain.Repositories.TableRepository
{
    public class TableRepository : ITableRepository
    {
        private static List<Table> tables = new List<Table>();
        private static readonly Random _rng = new Random();
        public TableRepository()
        {

            for (int i = 1; i < 26; ++i)
            {
                tables.Add(new Table(i, _rng.Next(2,10), TableState.FREE, new List<Order>()));
            }
            //nmg da stavim nikako drugacije da ih bude odredjen broj (ili ne znam)
        }

        public IEnumerable<Table> GetAllTables()
        {
            IEnumerable<Table> _tables = tables;
            return _tables;
        }

        public Table GetByID(int id) { 
            return tables[id];
        }

    }
}
