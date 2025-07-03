// Services/TakeATableServices/TakeATableClientService.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Repositories.WaiterRepository;
using Domain.Services;


namespace Services.TakeATableClientServices
{
    public class TakeATableClientService : ITakeATableClientService
    {
        private readonly IMakeAnOrder _orderService;
        private readonly IWaiterRepository _waiterRepo;
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _serverEndpoint;

        public TakeATableClientService(
            IMakeAnOrder orderService,
            IWaiterRepository waiterRepo,
            int localUdpPort)
        {
            _orderService = orderService;
            _waiterRepo = waiterRepo;
            // Kreiramo UdpClient JEDNOM, u konstruktoru
            _udpClient = new UdpClient(localUdpPort);
            // Server slusa na 4000
            _serverEndpoint = new IPEndPoint(IPAddress.Loopback, 4000);
        }

        public void TakeATable(int waiterId, int numGuests)
        {
            // Nikad ne raditi: using (_udpClient) { … }
            // Umesto toga samo šalji i primaš:
            string req = $"TAKE_TABLE;{waiterId};{numGuests}";
            var reqBytes = Encoding.UTF8.GetBytes(req);
            _udpClient.Send(reqBytes, reqBytes.Length, _serverEndpoint);

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] respBytes = _udpClient.Receive(ref remoteEP);
            string resp = Encoding.UTF8.GetString(respBytes);

            if (resp.StartsWith("TABLE_FREE;"))
            {
                int tableNum = int.Parse(resp.Split(';')[1]);
                Console.WriteLine($"Sto broj {tableNum} je slobodan!");
                _orderService.MakeAnOrder( tableNum,numGuests,waiterId);
            }
            else
            {
                Console.WriteLine("Svi stolovi su zauzeti, pokušajte kasnije.");
            }
        }
    }
}
