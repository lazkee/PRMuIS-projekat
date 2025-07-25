﻿using System;
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
        private readonly Socket _udpSocket;
        private readonly IPEndPoint _serverEp;

        public TakeATableClientService(
            IMakeAnOrder orderService,
            IWaiterRepository waiterRepo,
            int localUdpPort)
        {

            _udpSocket = new Socket(AddressFamily.InterNetwork,
                                SocketType.Dgram,
                                ProtocolType.Udp);
            _udpSocket.Bind(new IPEndPoint(IPAddress.Any, localUdpPort));
            _serverEp = new IPEndPoint(IPAddress.Loopback, 4000);
            _orderService = orderService;
        }

        public void TakeATable(int waiterId, int numGuests)
        {
            if (_udpSocket == null) Console.WriteLine("GRESKA: _udpSocket == null");
            if (_serverEp == null) Console.WriteLine("GRESKA: _serverEndpoint == null");

            var bynaryMes = Encoding.UTF8.GetBytes($"TAKE_TABLE;{waiterId};{numGuests};WAITER;0");
            _udpSocket.SendTo(bynaryMes, 0, bynaryMes.Length, SocketFlags.None, _serverEp);

            var buf = new byte[1024];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            int received = _udpSocket.ReceiveFrom(buf, ref remote);
            var resp = Encoding.UTF8.GetString(buf, 0, received);

            if (resp.StartsWith("TABLE_FREE;"))
            {
                int tableNum = int.Parse(resp.Split(';')[1]);
                Console.WriteLine($"Gosti smjesteni za sto broj {tableNum}!");
                _orderService.MakeAnOrder(tableNum, numGuests, waiterId);
            }
            else
            {
                Console.WriteLine("Svi stolovi su zauzeti, pokušajte kasnije.");
            }
        }
    }
}
