using System;
using System.Threading;
using Domain.Helpers;
using Domain.Services;
using Services.ServerServices;
using Services.TakeATableServices;
using Services.TakeOrdersFromWaiterServices;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            //inject WaiterRepository svugde gde treba, da bi Server mogao da proverava u kojem su stanju kad mu zatrebaju

            IReadService iReadService = new ServerReadTablesService();

            ITakeATableServerService iTakeTableServerService = new TakeATableServerService(iReadService);

            Thread serverTakeTableThread = new Thread(() => iTakeTableServerService.TakeATable());
            serverTakeTableThread.Start();

            ITakeOrdersFromWaiterService iTakeOrdersFromWaiterService = new TakeOrdersFromWaiterService(iReadService);
            Thread serverTakeOrdersFromWaiter = new Thread(() => iTakeOrdersFromWaiterService.ProcessAnOrder());
            serverTakeOrdersFromWaiter.Start();

            CreateClientInstance createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, "konobar");
            createClientInstance.BrojITipKlijenta(1, "kuvar");
            //createClientInstance.BrojITipKlijenta(1, "barmen");

            ////////////////////////////////za sada se threadovi ne terminiraju na kraju(server se ne gasi sam), nisam siguran jos kako cemo simulirati kraj, zavisi od toga kako ce ceo sistem raditi tj koliko ce biti konobara i sta sve oni mogu da rade, mozda ce biti da se server tj program gasi kada se svi konobari odjave
            //otici u WaiterManagementService


            //Console.WriteLine("end");
            //Console.ReadKey();
        }



    }
}
