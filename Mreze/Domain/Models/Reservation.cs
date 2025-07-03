using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Reservation
    {
        public int TableNumber { get; set; }

        public DateTime ReservationTime { get; set; }

        public DateTime ExpiryTime => ReservationTime + TimeSpan.FromSeconds(10);
    }
}
