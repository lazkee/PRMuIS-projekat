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
            sb.Append("\nWhat is ordered:\n");
            sb.Append("\"| Article name   | Article category | Article price |   status   |\"");
            foreach (Order o in orders)
            {
                sb.Append(o.ToString());
                sb.Append("/n");
            }

            return sb.ToString();
        }
    }
}
