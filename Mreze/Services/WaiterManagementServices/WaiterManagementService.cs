using Domain.Repositories.WaiterRepository;
using Domain.Services;
using System;
using System.Threading;

namespace Services.WaiterManagementServices {
    public class WaiterManagementService : IWaiterManagementService
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

        public void WaiterIsServing(int waiterId)
        {
            while (!_waiterRepo.HasOrderReady(waiterId))
            {
                Console.WriteLine("1. Take a new table:");
                Console.WriteLine("0. Close the waiter");
                Console.Write("Your instruction: ");
                var key = Console.ReadLine();

                if (key == "1")
                {
                    // 1) Obeleži konobara kao zauzetog
                    _waiterRepo.SetWaiterState(waiterId, true);

                    // 2) Uzmi broj gostiju i pošalji zahtev
                    Console.Write("How many guests per table: ");
                    if (!int.TryParse(Console.ReadLine(), out var numGuests))
                    {
                        Console.WriteLine("Unesi validan broj gostiju.");
                        _waiterRepo.SetWaiterState(waiterId, false);
                        continue;
                    }

                    _takeTableService.TakeATable(waiterId, numGuests);

                    //// 3) Čekamo da stigne READY zastavica
                    //Console.WriteLine("Čekam da porudzbina bude spremna...");
                    //while (!_waiterRepo.HasOrderReady(waiterId))
                    //{
                    //    Thread.Sleep(200);
                    //}

                    //// 4) Simulacija nošenja
                    //Console.WriteLine("Porudzbina je spremna! Nosim je…");
                    //Thread.Sleep(1500);

                    //// 5) Reset zastavice i označi konobara slobodnim
                    //_waiterRepo.ClearOrderReady(waiterId);
                    //_waiterRepo.SetWaiterState(waiterId, false);
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
