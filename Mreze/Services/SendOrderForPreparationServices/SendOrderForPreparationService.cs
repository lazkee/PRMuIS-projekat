using System.Collections.Generic;
using Domain.Models;
using Domain.Services;
using Domain.Repositories;
using Domain.Repositories.OrderRepository;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Threading;

namespace Services.SendOrderForPreparationServices
{
    public class SendOrderForPreparationService : ISendOrderForPreparation
    {   
        private FoodOrderRepository _foodOrderRepository = new FoodOrderRepository();
        private DrinkOrderRepository _drinkOrderRepository = new DrinkOrderRepository();

    public SendOrderForPreparationService(int numOfChefs, int numOfBarmens)
        {
            // Pokretanje radnika - kuvara
            for (int i = 0; i < numOfChefs; i++)
            {
                Task.Factory.StartNew(() => ProcessOrdersToStaff(_foodOrderRepository.GetAllOrders(), "Kuvar"), TaskCreationOptions.LongRunning);
            }

            // Pokretanje radnika - barmena
            for (int i = 0; i < numOfBarmens; i++)
            {
                Task.Factory.StartNew(() => ProcessOrdersToStaff(_drinkOrderRepository.GetAllOrders(), "Barmen"), TaskCreationOptions.LongRunning);
            }
        }

    public void SendOrder(int WaiterID,List<Order> orders)
        {
            List<Order> food = new List<Order>();
            List<Order> drinks = new List<Order>();
            foreach (var order in orders)
            {
                if(order.ArticleCategory == ArticleCategory.DRINK)
                {
                    drinks.Add(order);
                }
                else
                {
                    food.Add(order);
                }
            }
            //dodajemo u queue
            _foodOrderRepository.AddOrder(food);
            _drinkOrderRepository.AddOrder(drinks);

            //ProcessOrdersToStaff();




            //ovo ce imati if (== food) salje novu listu od ove porudzbine sa tcp za ovu sto obradjuje kuvar PrepareFoodService a ako je == drink onda ce slati drugu novu listu za servis koji barmen ima PrepareDrinkService ali ce ta 2 servisa morati da se pokrecu u Cook i Barmenu
            //morace oba da budu threada jer ce cekati sve vreme da im server posalje porudzbine 
            //i server ce morati da ima uvid da li su oni busy ili ne kao sto ima i za konobara

            //a pre toga za konobara jos fali jedan thread (vrv tcp a mozda moze i udp) koji ce stalno ocekivati da primi gotovu porudzbinu(i ispise samo da je doneo) i onda vrv jos jedan thread koji ocekuje racun od servera koji treba da ga izracuna
            
        }


        /// <summary>
        /// TODO
        ///pomocne metode koje bukvalno salju porudzbine gdje treba i kada treba 
        ///moram razmisliti kako da implementiramo nesto sto ce biti kao unique lock na os
        ///u sustini kad se barmen ili kuvar oslobodi da ono odma salje sledeci order ako queue nije prazan
        ///ali isto mora i da radi nakon dodavanju u queue gdje je on prvobitno bio prazan
        /// </summary>
       private void ProcessOrdersToStaff(BlockingCollection<List<Order>> order,string WorkerType)
        {
            // ovaj kod treba primjeniti u kuvaruy/sankeru kada primi poruku
            //foreach (var order in queue.GetConsumingEnumerable()) // Neće blokirati CPU, čeka dok ne dođe porudžbina
            //{
            //    Console.WriteLine($"{WorkerType} obrađuje porudžbinu ");
            //    Thread.Sleep(2000); // Simulacija vremena obrade
            //    Console.WriteLine($"{WorkerType} završio porudžbinu");
            //}

        }


    }
}
