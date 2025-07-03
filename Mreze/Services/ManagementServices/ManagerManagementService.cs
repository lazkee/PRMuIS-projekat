using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (clientType == ClientType.Manager) {

                while (!_managerRepository.GetManagerState(clientId))
                {

                    Console.WriteLine("1. Take a new table");
                    Console.WriteLine("2. Check my reservation");
                    Console.WriteLine("0. Close the waiter");
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
