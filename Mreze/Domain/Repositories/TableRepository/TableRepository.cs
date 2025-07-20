using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Models;

namespace Domain.Repositories.TableRepository
{
    public static class TableRepository
    {
        // jedinička, statička lista svih stolova
        private static readonly List<Table> _tables;
        private static readonly Random _rng = new Random();

        // Static konstruktor koji inicijalizuje 25 stolova
        static TableRepository()
        {
            _tables = new List<Table>();
            for (int i = 1; i <= 25; i++)
            {
                _tables.Add(new Table(
                    tableNumber: i,
                    numberOfGuests: _rng.Next(2, 10),
                    tableState: TableState.FREE,
                    orders: new List<Order>()
                ));
            }
        }

        
        public static IEnumerable<Table> GetAllTables()
            => _tables;

        
        public static void UpdateTable(Table table)
        {
            int idx = _tables.FindIndex(t => t.TableNumber == table.TableNumber);
            if (idx >= 0)
                _tables[idx] = table;
        }

        
        public static Table GetByID(int tableNumber)
            => _tables.FirstOrDefault(t => t.TableNumber == tableNumber);

        
        public static void ClearTable(int tableNumber)
        {
            var resetTable = new Table(
                tableNumber: tableNumber,
                numberOfGuests: _rng.Next(2, 10),
                tableState: TableState.FREE,
                orders: new List<Order>()
            );
            UpdateTable(resetTable);
        }
    }
}
