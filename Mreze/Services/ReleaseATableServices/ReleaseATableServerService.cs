using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Domain.Services;

namespace Services.ReleaseATableServices
{
    public class ReleaseATableServerService : IReleaseATableService

    {
        private readonly IReadService _readService;
        private readonly int _port;

        public ReleaseATableServerService(IReadService readService, int port)
        {
            _readService = readService ?? throw new ArgumentNullException(nameof(readService));
            _port = port;
        }

        public void ReleaseATable(int managerNumber = 0)
        {
            new Thread(() =>
            {
                var udp = new UdpClient(_port);
                var remoteEP = new IPEndPoint(IPAddress.Any, 0);
                Console.WriteLine($"[Server] Pokrenut servis za brisanje isteklih rezervacija UDP port {_port}.");

                while (true)
                {
                    try
                    {
                        var data = udp.Receive(ref remoteEP);
                        string message = Encoding.UTF8.GetString(data);

                        if (message.StartsWith("CANCEL_RESERVATION;"))
                        {
                            int tableNum = int.Parse(message.Split(';')[1]);
                            _readService.ReleaseTable(tableNum);
                            Console.WriteLine($"Sto broj {tableNum} je slobodan!\n");

                            string reply = $"CANCEL_OK;{tableNum}";
                            byte[] outData = Encoding.UTF8.GetBytes(reply);
                            udp.Send(outData, outData.Length, remoteEP);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
