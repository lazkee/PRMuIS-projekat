using System.Collections.Generic;
using System.Linq;
using System;
using Domain.Models;
using Domain.Repositories.TableRepository;
namespace Domain.Helpers
{
    public class CalculateTheBill
    {
        

        public string Calculate(int brStola)
        {
            string s = string.Empty;
            int suma = 0;
            List<Order> orders= TableRepository.GetByID(brStola).TableOrders;
            foreach (Order o in orders)
            {
                suma += (int)o._price;
                s += $"{o._articleName,-14}{o._price,-13}\n";
               
            }
            string ret = suma.ToString() + ";" + s;
            return ret;
        }

    }
}
