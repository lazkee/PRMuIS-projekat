using System.Collections.Generic;
using Domain.Models;
using Domain.Repositories.TableRepository;
namespace Domain.Helpers
{
    public class CalculateTheBill
    {
        private TableRepository repo = new TableRepository();
        public string Calculate(int brStola)
        {
            string s = string.Empty;
            int suma = 0;
            List<Order> orders = repo.GetByID(brStola).TableOrders;
            foreach (Order o in orders)
            {
                suma += (int)o._price;
                string.Join(s, o.ToString());
            }

            string.Join(s, suma.ToString());

            return s;
        }

    }
}
