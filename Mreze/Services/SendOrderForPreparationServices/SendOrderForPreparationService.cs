// Services/PrepareOrderServices/SendOrderForPreparationService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Repositories.OrderRepository;
using Domain.Services;

namespace Services.SendOrderForPreparationServices
{
    public class SendOrderForPreparationService : ISendOrderForPreparation
    {
        private readonly IClientDirectory _directory;
        private readonly IOrderRepository _foodRepo;
        private readonly IOrderRepository _drinkRepo;

        public SendOrderForPreparationService(
            IClientDirectory directory,
            IOrderRepository foodRepository,
            IOrderRepository drinkRepository)
        {
            _directory = directory;
            _foodRepo = foodRepository;
            _drinkRepo = drinkRepository;

            // Startujemo niti za kuvare i barmene
            new Thread(() => ProcessOrdersToStaff(_foodRepo, ClientType.Cook))
            {
                IsBackground = true
            }.Start();

            new Thread(() => ProcessOrdersToStaff(_drinkRepo, ClientType.Bartender))
            {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Enqueue-uje batch porudžbina za pripremu.
        /// </summary>
        public void SendOrder(int waiterId, int tableNumber, List<Order> orders)
        {
            // Dodajemo waiterId u svaki Order objekat
            foreach (var o in orders)
            {
                o._waiterId = waiterId;
                o._tableNumber = tableNumber;
            }

            // Razvrstavamo po repozitorijumima
            var foodBatch = orders.Where(o => o.ArticleCategory == ArticleCategory.FOOD).ToList();
            if (foodBatch.Any())
                _foodRepo.AddOrder(foodBatch);

            var drinkBatch = orders.Where(o => o.ArticleCategory == ArticleCategory.DRINK).ToList();
            if (drinkBatch.Any())
                _drinkRepo.AddOrder(drinkBatch);
        }

        /// <summary>
        /// Pozadinska nit: čeka batch porudžbina iz repozitorijuma
        /// i šalje ih odgovarajućem tipu radnika putem TCP protokola.
        /// </summary>
        private void ProcessOrdersToStaff(IOrderRepository repository, ClientType workerType)
        {
            Console.WriteLine($"[SERVER] Pokrećem dispatch thread za {workerType}");
            int nextClient = 0;

            while (true)
            {
                // 1) Preuzmi batch porudžbina (blokira dok repozitorijum ne sadrži ništa)
                var batch = repository.RemoveOrder();   // List<Order>
                if (batch == null || batch.Count == 0) continue;






                // 2) Pripremi jedinstvenu poruku: uzmemo tableNumber i waiterId iz prve stavke
                int tableNo = batch[0]._tableNumber;    // već popunjeno
                int waiterId = batch[0]._waiterId;       // već popunjeno



                // 3) Sastavi sadržaj batch-a kao niz imena, npr. "Karadjordjeva,Karadjordjeva,Burek"
                string content = string.Join(",", batch.Select(o => o._articleName));

                Console.WriteLine(
                    $"[SERVER] Dispatchujem cele porudžbine za sto {tableNo} " +
                    $"(konobar {waiterId}): {content}");

                // 4) Učitaj najnoviju listu registrovanih radnika
                var clients = _directory.GetByType(workerType).ToList();
                if (!clients.Any())
                {
                    Console.WriteLine($"[WARN] Nema registrovanih {workerType}-a za pripremu.");
                    Thread.Sleep(500);
                    continue;
                }

                // 5) Round-robin ili uvek istog radnika
                var clientInfo = clients[nextClient];
                nextClient = (nextClient + 1) % clients.Count;

                // 6) Pošalji **jednu** PREPARE poruku sa kompletom batch-a
                //    Format: PREPARE;{tableNo};{waiterId};{content}\n
                string msg = $"PREPARE;{tableNo};{waiterId};{content}\n";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                try
                {
                    clientInfo.Socket.GetStream().Write(data, 0, data.Length);
                    Console.WriteLine(
                        $"[SERVER] Poslato {workerType}#{clientInfo.Id}: {msg.Trim()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[ERROR] Neuspelo slanje {workerType}#{clientInfo.Id}: {ex.Message}");
                }
            }
        }

    }
}
