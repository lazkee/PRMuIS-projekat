using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Models;
using Domain.Repositories.TableRepository;
using Domain.Services;

namespace Services.ServerServices
{
    public class ServerReadTablesService : IReadService
    {
        
        public IEnumerable<Table> GetAllTables()
        {
            return TableRepository.GetAllTables();
        }

        public int GetFreeTableFor(int numGuests)
        {
            var free = TableRepository.GetAllTables()
                .FirstOrDefault(t => t.TableState == TableState.FREE && t.Capacity >= numGuests);
            return free != null ? free.TableNumber : -1;
            //return free.TableNumber;
        }

        public void OccupyTable(int tableNumber, int waiterId)
        {
            var table = TableRepository.GetAllTables().FirstOrDefault(t => t.TableNumber == tableNumber);
            if (table != null)
            {
                table.TableState = TableState.BUSY;
                table.OccupiedBy = waiterId;
            }
        }

        public void ReleaseTable(int tableNumber)
        {
            var table = TableRepository.GetAllTables().FirstOrDefault(t => t.TableNumber == tableNumber);
            if(table != null)
            {
                table.TableState = TableState.FREE;
                Console.WriteLine($"Sto broj {tableNumber} je {table.TableState}");
            }
        }

    }
}
