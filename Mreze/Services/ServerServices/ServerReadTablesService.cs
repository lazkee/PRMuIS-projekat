using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Domain.Models;
using Domain.Repositories.TableRepository;
using Domain.Services;

namespace Services.ServerServices
{
    public class ServerReadTablesService : IReadService
    {
        ITableRepository tables = new TableRepository();
        

        

        

       

        public IEnumerable<Table> GetAllTables(){
            return tables.GetAllTables();
        }

        public int GetFreeTableFor(int numGuests)
        {
            var free = tables.GetAllTables()
                .FirstOrDefault(t => t.TableState==TableState.FREE && t.Capacity >= numGuests);
            //return free != null ? free.TableNumber : -1;
            return free.TableNumber;
        }

        public void OccupyTable(int tableNumber, int waiterId)
        {
            var table = tables.GetAllTables().FirstOrDefault(t => t.TableNumber == tableNumber);
            if (table != null)
            {
                table.TableState = TableState.BUSY;
                table.OccupiedBy = waiterId;
            }
        }
    }
}

        