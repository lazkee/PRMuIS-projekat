using System;
using System.Collections.Generic;
using System.Threading;
using Domain.Models;
using Domain.Repositories.OrderRepository;
using Services;  // za NotificationService
using Domain.Enums;
using Domain.Services;
using Services.NotificationServices;

namespace Services.SendOrderForPreparationServices
{
    /// <summary>
    /// Servis za dodelu porudžbina kuvarima i barmenima.
    /// </summary>
    public class SendOrderForPreparationService : ISendOrderForPreparation
    {
        private readonly IOrderRepository _foodOrderRepository;
        private readonly IOrderRepository _drinkOrderRepository;
        private readonly NotificationService _notificationService;

        /// <summary>
        /// Inicijalizuje repozitorijume i pokreće radne niti.
        /// </summary>
        /// <param name="numOfChefs">Broj kuvara.</param>
        /// <param name="numOfBarmens">Broj barmena.</param>
        /// <param name="notificationService">Servis za slanje notifikacija konobarima.</param>
        public SendOrderForPreparationService(
            int numOfChefs,
            int numOfBarmens,
            NotificationService notificationService)
        {
            _foodOrderRepository = new FoodOrderRepository();
            _drinkOrderRepository = new DrinkOrderRepository();
            _notificationService = notificationService;

            // Pokretanje niti za kuvare
            for (int i = 0; i < numOfChefs; i++)
            {
                new Thread(() => ProcessOrdersToStaff(_foodOrderRepository, ClientType.Cook))
                { IsBackground = true }
                    .Start();
            }

            // Pokretanje niti za barmene
            for (int i = 0; i < numOfBarmens; i++)
            {
                new Thread(() => ProcessOrdersToStaff(_drinkOrderRepository, ClientType.Bartender))
                { IsBackground = true }
                    .Start();
            }
        }

        /// <summary>
        /// Prima porudžbinu od konobara i enqueue-uje u odgovarajući repozitorijum.
        /// </summary>
        /// <param name="WaiterID">ID konobara.</param>
        /// <param name="orders">Lista stavki porudžbine.</param>
        public void SendOrder(int WaiterID, List<Order> orders)
        {
            var food = new List<Order>();
            var drinks = new List<Order>();

            foreach (var order in orders)
            {
                // Ubacujemo ID konobara u samu porudžbinu radi notifikacije
                order._waiterId = WaiterID;

                if (order.ArticleCategory == ArticleCategory.DRINK)
                    drinks.Add(order);
                else
                    food.Add(order);
            }

            if (food.Count > 0)
                _foodOrderRepository.AddOrder(food);
            if (drinks.Count > 0)
                _drinkOrderRepository.AddOrder(drinks);
        }

        /// <summary>
        /// Radna nit koja konzumira porudžbine iz repozitorijuma i simulira obradu.
        /// </summary>
        private void ProcessOrdersToStaff(IOrderRepository repository, ClientType workerType)
        {
            while (true)
            {
                // Uklanja sledeću porudžbinu iz reda (blokira ako je prazan)
                var batch = repository.RemoveOrder();
                foreach (var order in batch)
                {
                    Console.WriteLine($"{workerType} preuzima porudžbinu: Sto={order._tableNumber}, Artikal={order._articleName}");
                    // Simulacija vremena pripreme
                    Thread.Sleep(2000);
                    Console.WriteLine($"{workerType} završio porudžbinu: Sto={order._tableNumber}, Artikal={order._articleName}");

                    // Obaveštavamo server/konobara da je porudžbina gotova
                    _notificationService.NotifyOrderReady(order._tableNumber, order._waiterId);
                }
            }
        }
    }
}
