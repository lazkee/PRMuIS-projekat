using System;
using System.Threading;
using Domain.Repositories.ManagerRepository;
using Domain.Repositories.WaiterRepository;
using Services.MakeAnOrderServices;
using Services.ManagementServices;
using Services.ReleaseATableServices;
using Services.TakeATableServices;

namespace Manager
{
    public class Manager
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"[FATAL] {((Exception)e.ExceptionObject).Message}");
            };

            Console.WriteLine($"Manager number #{args[1]},");
            Console.WriteLine($"clientId #{args[0]}");
            Console.WriteLine($"Port #{args[2]}");

            int.TryParse(args[0], out int managerId);
            int.TryParse(args[1], out int managerNumber);
            int.TryParse(args[2], out int udpPort);

            var managerRepo = new ManagerRepository(2);
            var managerManagementService = new ManagerManagementService(managerRepo);
            new Thread(() => {
                managerManagementService.TakeOrReserveATable(managerNumber, Domain.Enums.ClientType.Manager);
            }).Start(); //IsBackground = true ovo ne treba ovde jer sa zatvori klijent onda

            var releaseATableManagerService = new ReleaseATableManagerService(managerRepo, 4001);
            releaseATableManagerService.ReleaseATable(managerNumber); //jako je bitno da ovde i gore bude managerNumber a ne clientId

            //Console.ReadKey();
        }
    }
}
