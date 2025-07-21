using System;
using Domain.Enums;
using Domain.Repositories.ManagerRepository;
using Domain.Services;

namespace Services.ManagementServices
{
    public class ManagerManagementService : IManagementService
    {
        private readonly IManagerRepository _managerRepository;

        public ManagerManagementService(IManagerRepository managerRepository)
        {
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
                    Console.Write("Unesi instrukciju: ");
                    var key = Console.ReadLine();

                    try
                    {
                        if (key.Equals("1"))
                        {
                            _managerRepository.SetManagerState(clientId, true);

                            Console.Write("Unesite broj gostiju: ");
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
                            Console.Write("Unesite broj rezervacije: ");
                            if (!int.TryParse(Console.ReadLine(), out var reservationNumber))
                            {
                                Console.WriteLine($"Rezervacija je broj! ({reservationNumber})\n");
                                _managerRepository.SetManagerState(clientId, false);
                                continue;
                            }

                            bool valid = _managerRepository.CheckReservation(reservationNumber);
                            if (!valid)
                            {
                                Console.WriteLine($"Nema rezervisanog stola za broj {reservationNumber}!\n");
                                _managerRepository.SetManagerState(clientId, false);
                                continue;
                            }
                            else
                            {
                                var expireDate = _managerRepository.GetExpireDate(reservationNumber);
                                Console.WriteLine($"Rezervacija {reservationNumber} je validna do {expireDate}\n");
                            }

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
