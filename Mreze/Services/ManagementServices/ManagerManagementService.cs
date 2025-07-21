using System;
using Domain.Enums;
using Domain.Repositories.ManagerRepository;
using Domain.Services;

namespace Services.ManagementServices
{
    public class ManagerManagementService : IManagementService
    {
        //private readonly ITakeATableClientService _takeATableManagerService;
        private readonly IManagerRepository _managerRepository;

        public ManagerManagementService(IManagerRepository managerRepository)
        {
            //_takeATableManagerService = takeATableManagerService ?? throw new ArgumentNullException(nameof(takeATableManagerService));
            _managerRepository = managerRepository ?? throw new ArgumentNullException(nameof(managerRepository));
        }

        public void TakeOrReserveATable(int clientId, ClientType clientType)
        {
            if (clientType == ClientType.Manager)
            {

                while (!_managerRepository.GetManagerState(clientId))
                {

                    Console.WriteLine("1. Napravi rezervaciju");
                    Console.WriteLine("2. Proveri rezervaciju");
                    Console.WriteLine("0. Ugasi Menadzera");
                    Console.Write("Your instruction: ");
                    var key = Console.ReadLine();

                    try
                    {
                        if (key.Equals("1"))
                        {
                            _managerRepository.SetManagerState(clientId, true);

                            Console.Write("How many guests per table: ");
                            if (!int.TryParse(Console.ReadLine(), out var numGuests) || numGuests > 10 || numGuests < 1)
                            {
                                Console.WriteLine("Unesi validan broj gostiju (max 10)!\n");
                                _managerRepository.SetManagerState(clientId, false);
                                continue;
                            }

                            _managerRepository.RequestFreeTable(clientId, 4000, numGuests);
                            _managerRepository.SetManagerState(clientId, false);

                        }
                        else if (key.Equals("2"))
                        {
                            _managerRepository.SetManagerState(clientId, true);
                            Console.Write("Your reservation number: ");
                            if (!int.TryParse(Console.ReadLine(), out var reservationNumber))
                            {
                                Console.WriteLine($"Reservation is a number! ({reservationNumber})\n");
                                _managerRepository.SetManagerState(clientId, false);
                                continue;
                            }

                            bool valid = _managerRepository.CheckReservation(reservationNumber);
                            if (!valid)
                            {
                                Console.WriteLine($"No reserved tables for {reservationNumber}!\n");
                                _managerRepository.SetManagerState(clientId, false);
                                continue;
                            }
                            else
                            {
                                var expireDate = _managerRepository.GetExpireDate(reservationNumber);
                                Console.WriteLine($"Reservation {reservationNumber} valid until {expireDate}\n");
                            }

                            /* int tableNumber = _managerRepository.GetTableNumber(reservationNumber);

                             string message = $"GUESTS_ARRIVED:{reservationNumber}:{tableNumber}";
                             byte[] data = Encoding.UTF8.GetBytes(message);

                             Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                             IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4002);

                             socket.SendTo(data, endPoint);
                             socket.Close();

                             Console.WriteLine("Notification sent to server via raw socket: guests arrived.");*/

                            //odavde se salje preko udp na 4002 serveru da su gosti stigli za stol

                            _managerRepository.SetManagerState(clientId, false);

                        }
                        else if (key.Equals("0"))
                        {
                            Console.WriteLine("Zatvaram menadzera…");
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Unesi 0 ili 1!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] {ex.Message}");
                        _managerRepository.SetManagerState(clientId, false);
                    }
                }
            }
        }

    }
}
