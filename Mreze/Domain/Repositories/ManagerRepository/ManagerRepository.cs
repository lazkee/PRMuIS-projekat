using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Repositories.ManagerRepository
{
    public class ManagerRepository : IManagerRepository
    {
        private static readonly ConcurrentDictionary<int, bool> _managerBusy
            = new ConcurrentDictionary<int, bool>();
        private static readonly ConcurrentDictionary<int, Reservation> _reservationNumber = new ConcurrentDictionary<int, Reservation>();

        private static int key_value = 200;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public ManagerRepository(int numberOfManagers)
        {
            if (numberOfManagers < 1)
                throw new ArgumentException("Potrebno je bar 1 konobar.", nameof(numberOfManagers));

            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        for (int i = 1; i <= numberOfManagers; i++)
                        {
                            _managerBusy[i] = false;
                        }
                        _initialized = true;
                    }
                }
            }
        }

        public bool GetManagerState(int managerId)
        {
            ValidateId(managerId);
            return _managerBusy[managerId];
        }

        private void ValidateId(int managerId)
        {
            if (!_managerBusy.ContainsKey(managerId))
                throw new ArgumentOutOfRangeException(
                    nameof(managerId),
                    $"ID menadzera {managerId} nije u opsegu 1..{_managerBusy.Count}.");
        }

        public void SetManagerState(int managerId, bool isBusy)
        {
            ValidateId(managerId);
            _managerBusy[managerId] = isBusy;
        }

        public void RequestFreeTable(int managerId, int serverPort, int numGuests)
        {
            using (var client = new UdpClient())
            {
                string message = $"TAKE_TABLE;{managerId};{numGuests}"; //postoji i ideja da bude i clientType poslat, ali onda mora da se prosledjuje i da li je reserved ili busy samo, sto otezava a realno svejedno da li je busy ili reserved samo, svakako je zauzet sto
                byte[] data = Encoding.UTF8.GetBytes(message);
                client.Send(data, data.Length, "127.0.0.1", serverPort);

                var serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
                var response = client.Receive(ref serverEndpoint);      //ovo blokira
                string responseText = Encoding.UTF8.GetString(response);

                if (responseText.StartsWith("TABLE_FREE;"))
                {
                    int tableNum = int.Parse(responseText.Split(';')[1]);
                    _reservationNumber[key_value] = new Reservation{ TableNumber = tableNum, ReservationTime = DateTime.Now };
                    Console.WriteLine($"Table number {tableNum} is reserved, your reserve number is {key_value}!\n");
                    ++key_value;
                    return;
                }
                else if (responseText == "TABLE_BUSY")
                {
                    Console.WriteLine("All tables are busy at the moment!\n");
                    return;
                }

                Console.WriteLine("Unexpected server response.\n");
                return;
            }
        }

        public void RemoveReservation(int reservationId)
        {
            _reservationNumber.TryRemove(reservationId, out var reservation);
        }

        public IEnumerable<(int Key, Reservation Table)> GetExpiredReservations()
        {
            var now = DateTime.Now;
            return _reservationNumber
                .Where(kvp => now > kvp.Value.ExpiryTime)
                .Select(kvp => (kvp.Key, kvp.Value));
        }

        public bool CheckReservation(int reservationId) {

            if (_reservationNumber[reservationId] == null) {
                return false;
            }
            return true;
        }

        //metoda da proverava broj rezervacije i da javi serveru da su gosti stigli

    }
}
