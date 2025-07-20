using System;

namespace Domain.Models
{
    public class Reservation
    {
        public int TableNumber { get; set; }

        public DateTime ReservationTime { get; set; }

        public DateTime ExpiryTime => ReservationTime + TimeSpan.FromSeconds(60);
    }
}
