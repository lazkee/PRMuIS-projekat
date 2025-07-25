﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Repositories.ManagerRepository;
using Domain.Services;

namespace Services.TakeATableServices
{
    public class TakeATableServerService : ITakeATableServerService
    {
        private readonly IReadService _readService;
        private readonly Socket _serverSocketUdp;
        private EndPoint _remoteEp;
        private IManagerRepository _managerRepository;

        public TakeATableServerService(IReadService readService, IManagerRepository managerRepo, int listenPort = 4000)
        {
            _readService = readService;
            _serverSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverSocketUdp.Bind(new IPEndPoint(IPAddress.Loopback, 4000));
           

            _remoteEp = new IPEndPoint(IPAddress.Any, 0);
            _managerRepository = managerRepo;
        }

        public void TakeATable()
        {

            while (true)
            {
                var buf = new byte[1024];
                int len = _serverSocketUdp.ReceiveFrom(buf, ref _remoteEp);
                var msg = Encoding.UTF8.GetString(buf, 0, len);

                var parts = msg.Split(';');
                int waiterId = int.Parse(parts[1]);
                int numGuests = int.Parse(parts[2]);
                string clientType = parts[3];
                int reservationNumber = int.Parse(parts[4]);
                string reply;

                if (numGuests <= 10)
                {
                    int freeTable = _readService.GetFreeTableFor(numGuests);

                    if (freeTable >= 0)
                    {
                        reply = $"TABLE_FREE;{freeTable}";

                        _readService.OccupyTable(freeTable, waiterId);
                        if (clientType == "MANAGER")
                        {
                            _managerRepository.AddNewReservationForServer(reservationNumber, freeTable);
                            Console.WriteLine($"[Server] Sto broj {freeTable} je sada rezervisan");
                        }
                        else
                        {
                            Console.WriteLine($"[Server] Sto broj {freeTable} je sada zauzet");
                        }
                       
                        

                    }
                    else
                    {
                        reply = "TABLE_BUSY";
                    }
                }
                else { reply = "TABLE_BUSY"; }

                var outData = Encoding.UTF8.GetBytes(reply);
                _serverSocketUdp.SendTo(outData, 0, outData.Length, SocketFlags.None, _remoteEp);
            }
        }
    }
}
