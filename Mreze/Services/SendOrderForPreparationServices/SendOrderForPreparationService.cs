using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            new Thread(() => ProcessOrdersToStaff(_foodRepo, ClientType.Cook))
            {
                IsBackground = true
            }.Start();

            new Thread(() => ProcessOrdersToStaff(_drinkRepo, ClientType.Bartender))
            {
                IsBackground = true
            }.Start();
        }

        public void SendOrder(int waiterId, int tableNumber, List<Order> orders)
        {
            foreach (var o in orders)
            {
                o._waiterId = waiterId;
                o._tableNumber = tableNumber;
            }

            var foodBatch = orders.Where(o => o.ArticleCategory == ArticleCategory.FOOD).ToList();
            if (foodBatch.Any())
                _foodRepo.AddOrder(foodBatch);

            var drinkBatch = orders.Where(o => o.ArticleCategory == ArticleCategory.DRINK).ToList();
            if (drinkBatch.Any())
                _drinkRepo.AddOrder(drinkBatch);
        }

        public void ProcessOrdersToStaff(IOrderRepository repository, ClientType workerType)
        {
            Console.WriteLine($"[SERVER] Pokrećem dispatch thread za {workerType}");
            var rnd = new Random();
            var formatter = new BinaryFormatter();

            while (true)
            {

                var batch = repository.RemoveOrder();   
                if (batch == null || batch.Count == 0)
                {
                    Thread.Sleep(50);
                    continue;
                }

                var clientList = _directory
                    .GetByType(workerType)
                    .ToList();

                if (clientList.Count == 0)
                {
                    Console.WriteLine($"[SERVER] Nema aktivnih {workerType}-a za dispatch.");
                    Thread.Sleep(200);
                    continue;
                }

                int tableNo = batch[0]._tableNumber;
                int waiterId = batch[0]._waiterId;

                byte[] orderData = new byte[8192];
                using (var ms = new MemoryStream())
                {
                    formatter.Serialize(ms, batch);
                    orderData = ms.ToArray();
                }

                string b64 = Convert.ToBase64String(orderData);
                string protocol = $"PREPARE;{tableNo};{waiterId};{b64}\n";
                byte[] payload = Encoding.UTF8.GetBytes(protocol);

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
