using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Services;

namespace Services.TakeATableServices
{
    public class TakeATableServerService : ITakeATableServerService
    {
        private readonly IReadService _readService;
        private readonly UdpClient _udpServer;

        public TakeATableServerService(IReadService readService, int listenPort = 4000)
        {
            _readService = readService;
            _udpServer = new UdpClient(listenPort);
            Console.WriteLine($"UDP TableListener pokrenut na portu {listenPort}.");
        }

        public void TakeATable()
        {
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                // 1) Čitanje zahteva
                var data = _udpServer.Receive(ref remoteEP);
                var msg = Encoding.UTF8.GetString(data);
                // ocekivani format: TAKE_TABLE;{waiterId};{numGuests}
                var parts = msg.Split(';');
                int waiterId = int.Parse(parts[1]);
                int numGuests = int.Parse(parts[2]);
                // 2) Provera maksimalnog kapaciteta stola
                string reply;
                if (numGuests <= 10)
                {
                    // 2.1) Provera slobodnog stola
                    int freeTable = _readService.GetFreeTableFor(numGuests);

                    if (freeTable >= 0)
                    {
                        reply = $"TABLE_FREE;{freeTable}";
                        // Obelezi sto kao zauzet
                        _readService.OccupyTable(freeTable, waiterId);
                    }
                    else
                    {
                        reply = "TABLE_BUSY";
                    }
                }
                else { reply = "TABLE_BUSY"; }
                // 3) Slanje odgovora
                var outData = Encoding.UTF8.GetBytes(reply);
                _udpServer.Send(outData, outData.Length, remoteEP);
                Console.WriteLine($"Odgovor klijentu {remoteEP.Address}:{remoteEP.Port} → {reply}");
            }
        }
    }
}
