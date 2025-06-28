using System;
using System.Net.Sockets;
using Domain.Repositories.WaiterRepository;
using Domain.Services;
using Services.MakeAnOrderServices;
using Services.TakeATableServices;
using Services.WaiterManagementServices;

namespace Client
{
    public class Client
    {
        //private readonly Socket _workerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //public Socket WorkerSocket => _workerSocket;


        static void Main(string[] args)
        {

            Console.WriteLine($"Waiter number #{args[1]},");
            Console.WriteLine($"clientId #{args[0]}");

            int.TryParse(args[1], out int numberOfWaiters);
            int.TryParse(args[0], out int WaiterID);

            IWaiterRepository iWaiterRepository = new WaiterRepository(numberOfWaiters);
            IMakeAnOrder iMakeAnOrderWaiterService = new MakeAnOrderWaiterService(iWaiterRepository);
            //ovo ce se samo preskociti, da se ne bi Repository kreirao vise puta, pogledati WaiterRepository konstruktor, prvi put se konstruise u CreateClientInstance i onda nikad vise(a treba nam inject da ga koristimo u iTakeATableClientService)
            ITakeATableClientService iTakeATableClientService = new TakeATableClientService(iMakeAnOrderWaiterService, iWaiterRepository);

            IWaiterManagementService iWaiterManagementService = new WaiterManagementService(iTakeATableClientService);

            iWaiterManagementService.WaiterIsServing(WaiterID);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
