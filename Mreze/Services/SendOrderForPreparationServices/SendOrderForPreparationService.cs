// Services/PrepareOrderServices/SendOrderForPreparationService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Repositories.OrderRepository;
using Domain.Repositories.TableRepository;
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
        public void ProcessOrdersToStaff(IOrderRepository repository, ClientType workerType)
        {
            Console.WriteLine($"[SERVER] Pokrećem dispatch thread za {workerType}");
            var rnd = new Random();
            var formatter = new BinaryFormatter();

            while (true)
            {
                // 1) Uzmi batch porudžbina
                var batch = repository.RemoveOrder();   // List<Order>
                if (batch == null || batch.Count == 0)
                {
                    Thread.Sleep(50);
                    continue;
                }

                // 2) Dohvati sve trenutno registrovane klijente željenog tipa
                var clientList = _directory
                    .GetByType(workerType)
                    .ToList();

                if (clientList.Count == 0)
                {
                    Console.WriteLine($"[SERVER] Nema aktivnih {workerType}-a za dispatch.");
                    Thread.Sleep(200);
                    continue;
                }

                // 3) Pripremi Table objekat
                //int tableNo = batch[0]._tableNumber;
                //int waiterId = batch[0]._waiterId;
                //var table = new Table(
                //    tableNumber: tableNo,
                //    numberOfGuests: 0,                 // ako nemaš broj gostiju, možeš staviti 0
                //    tableState: TableState.BUSY,
                //    orders: batch);
                

                int tableNo = batch[0]._tableNumber;
                int waiterId = batch[0]._waiterId;
                //Table table = tableRepository.GetByID(tableNo);
                

                // 4) Serijalizuj ga u bajt niz
                byte[] orderData = new byte[8192];
                using (var ms = new MemoryStream())
                {
                    formatter.Serialize(ms, batch);
                    orderData = ms.ToArray();
                }

                // 5) Neka poruka bude: "PREPARE;{tableNo};{waiterId};{Base64(tablica)}\n"
                string b64 = Convert.ToBase64String(orderData);
                string protocol = $"PREPARE;{tableNo};{waiterId};{b64}\n";
                byte[] payload = Encoding.UTF8.GetBytes(protocol);

                // 6) Izaberi nasumičnog radnika i pošalji mu
                var clientInfo = clientList[rnd.Next(clientList.Count)];
                try
                {
                    clientInfo.Socket.Send(payload);
                    Console.WriteLine(
                        $"[SERVER] Poslato {workerType}#{clientInfo.Id}: PREPARE;{tableNo};{waiterId};(serialized {batch.Count} stavki)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[ERROR] Neuspešno slanje {workerType}#{clientInfo.Id}: {ex.Message}");
                }
            }
        }




    }
}
