using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
                //Console.WriteLine("[Server] pokrenut servis za brisanje isteklih rezervacija.");

                while (true)
                {
                    _managerRepository.SetManagerState(managerNumber, true);
                    var expired = _managerRepository.GetExpiredReservations();
                    foreach (var expiredRes in expired)
                    {
                        using (var client = new UdpClient())
                        {
                            string msg = $"CANCEL_RESERVATION;{expiredRes.Table.TableNumber}";
                            byte[] data = Encoding.UTF8.GetBytes(msg);
                            client.Send(data, data.Length, "127.0.0.1", _releasePort);

                            client.Client.ReceiveTimeout = 1000;
                            try
                            {
                                var ep = new IPEndPoint(IPAddress.Any, 0);
                                var response = client.Receive(ref ep);
                                string reply = Encoding.UTF8.GetString(response);

                                _managerRepository.RemoveReservation(expiredRes.Key);
                                Console.WriteLine($"\n[Brisanje rezervacija] Rezervacija #{expiredRes.Key} za sto {expiredRes.Key} je istekla i obrisana je.");
                            }
                            catch { }
                        }
                    }
                    _managerRepository.SetManagerState(managerNumber, false);
                    Thread.Sleep(10000);
                }

            })
            { IsBackground = true }.Start();
        }
    }
}
