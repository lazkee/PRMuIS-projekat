using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Repositories.WaiterRepository;
using Domain.Services;


namespace Services.TakeATableServices
{
    public class TakeATableClientService : ITakeATableClientService
    {
        private readonly IMakeAnOrder _orderService;
        private readonly IWaiterRepository _waiterRepo;
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _serverUdpEndpoint;

        public TakeATableClientService(
            IMakeAnOrder orderService,
            IWaiterRepository waiterRepo,
            int _serverUdport)
        {
            _orderService = orderService;
            _waiterRepo = waiterRepo;
            _udpClient = new UdpClient();
            // Pretpostavljeni server UDP port za raspodelu stolova (npr. 4000)
            _serverUdpEndpoint = new IPEndPoint(IPAddress.Loopback, _serverUdport);
        }

        public void TakeATable(int waiterId, int numGuests)
        {
            // 1) Pošalji UDP zahtev: "TAKE_TABLE;{waiterId};{numGuests}"
            Console.WriteLine($"Usluzuje konobar #{waiterId}");
            string request = $"TAKE_TABLE;{waiterId};{numGuests}";
            byte[] reqBytes = Encoding.UTF8.GetBytes(request);
            try
            {


                _udpClient.Send(reqBytes, reqBytes.Length, _serverUdpEndpoint);

                // 2) Prihvati odgovor UDP: "TABLE_FREE;{tableNumber}" ili "TABLE_BUSY"
                IPEndPoint remoteEP = null;
                byte[] respBytes = _udpClient.Receive(ref remoteEP);
                string response = Encoding.UTF8.GetString(respBytes);

                if (response.StartsWith("TABLE_FREE;"))
                {
                    int tableNum = int.Parse(response.Split(';')[1]);
                    Console.WriteLine($"Gosti smjesteni za sto broj {tableNum}!");
                    // 3) Pređi na poručivanje preko TCP
                    _orderService.MakeAnOrder(tableNum, numGuests, waiterId);
                }
                else
                {
                    Console.WriteLine("Nema slobodnih odgovarajucih stolova.");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska pri kontaktu sa serverp:{ex.Message}");
                return;
            }
        }
    }
}