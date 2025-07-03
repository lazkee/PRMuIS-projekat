using System;
using Domain.Enums;
using Domain.Repositories.ManagerRepository;
using Domain.Repositories.WaiterRepository;
using Domain.Services;

namespace Services.WaiterManagementServices
{
    public class WaiterManagementService : IManagementService
    {
        private readonly ITakeATableClientService _takeTableService;
        private readonly IWaiterRepository _waiterRepo;

        public WaiterManagementService(
            ITakeATableClientService takeTableService,
            IWaiterRepository waiterRepo)
        {
            _takeTableService = takeTableService
                ?? throw new ArgumentNullException(nameof(takeTableService));
            _waiterRepo = waiterRepo
                ?? throw new ArgumentNullException(nameof(waiterRepo));
        }

        public void TakeOrReserveATable(int clientId, ClientType clientType)
        {
            if (clientType == ClientType.Waiter)
            {

                while (!_waiterRepo.HasOrderReady(clientId))
                {

                    Console.WriteLine("1. Take a new table:");
                    Console.WriteLine("0. Close the waiter");
                    Console.Write("Your instruction: ");
                    var key = Console.ReadLine();

                    if (key == "1")
                    {
                        // 1) Obeleži konobara kao zauzetog
                        _waiterRepo.SetWaiterState(clientId, true);

                        // 2) Uzmi broj gostiju i pošalji zahtev
                        Console.Write("How many guests per table: ");
                        if (!int.TryParse(Console.ReadLine(), out var numGuests))
                        {
                            Console.WriteLine("Unesi validan broj gostiju.");
                            _waiterRepo.SetWaiterState(clientId, false);
                            continue;
                        }

                        _takeTableService.TakeATable(clientId, numGuests);

                    }
                    else if (key == "0")
                    {
                        Console.WriteLine("Zatvaram konobara…");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unesi 0 ili 1!");
                    }
                }
            }
        }
    }
}
