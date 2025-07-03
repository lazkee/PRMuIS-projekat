using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories.ManagerRepository;
using Domain.Services;

namespace Services.ReleaseATableServices
{
    public class ReleaseATableManagerService : IReleaseATableService
    {
        private readonly IManagerRepository _managerRepository;
        private readonly int _releasePort;

        public ReleaseATableManagerService(IManagerRepository managerRepository, int releasePort)
        {
            _managerRepository = managerRepository;
            _releasePort = releasePort;
        }

        public void ReleaseATable(int managerNumber)
        {
            new Thread(() =>
            {
                //Console.WriteLine("[Server] ReservationExpiryService started.");

                while (true)
                {
                    _managerRepository.SetManagerState(managerNumber, true);
                    var expired = _managerRepository.GetExpiredReservations();
                    foreach (var expiredRes in expired)
                    {
                        // Inform table server to release the table (UDP to 4001)
                        using (var client = new UdpClient())
                        {
                            string msg = $"CANCEL_RESERVATION;{expiredRes.Table.TableNumber}";  //Key je table number
                            byte[] data = Encoding.UTF8.GetBytes(msg);
                            client.Send(data, data.Length, "127.0.0.1", _releasePort);

                            // optionally read confirmation (non-blocking)
                            client.Client.ReceiveTimeout = 1000;
                            try
                            {
                                var ep = new IPEndPoint(IPAddress.Any, 0);
                                var response = client.Receive(ref ep);
                                string reply = Encoding.UTF8.GetString(response);
                                //Console.WriteLine($"[Auto-Cancel] {reply}");
                                _managerRepository.RemoveReservation(expiredRes.Key);
                                Console.WriteLine($"\n[Auto-Cancel] Reservation #{expiredRes.Key} for table {expiredRes.Key} expired and was removed.");
                            }
                            catch { }
                        }
                    }
                    _managerRepository.SetManagerState(managerNumber, false);
                    Thread.Sleep(10_000); // check every 10 seconds
                }

            })
            { IsBackground = true }.Start();
        }
    }
}
