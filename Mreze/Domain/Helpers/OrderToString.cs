using System.Collections.Generic;
using System.Text;
using Domain.Models;

namespace Domain.Helpers
{
    internal class OrderToString
    {
        public StringBuilder sb = new StringBuilder();

        private string Convert(List<Order> orders)
        {
            sb.Append("\nNarudzba:\n");
            sb.Append("\"|  Naziv artikla | Kategorija artikla | Cijena artikla |   Status   |\"");
            foreach (Order o in orders)
            {
                sb.Append(o.ToString());
                sb.Append("/n");
            }

            return sb.ToString();
            
            




        }
    }
}
