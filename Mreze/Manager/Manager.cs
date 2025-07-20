using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
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

            // a) Kreiramo TCP socket i povežemo se
            var sock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(IPAddress.Loopback, 5000));

            // b) Pošaljemo REGISTER;{waiterId};Waiter;{udpPort}\n
            string regMsg = $"REGISTER;{managerId};Manager;{udpPort}\n";
            sock.Send(Encoding.UTF8.GetBytes(regMsg));

            // c) Prihvatimo ACK liniju “REGISTERED\n”
            var tmp = new byte[1];


            byte[] ackbytes = new byte[1024];
            int bytesRecieved = sock.Receive(ackbytes);
            string ack = Encoding.UTF8.GetString(ackbytes, 0, bytesRecieved).Trim();
            if (ack != "REGISTERED")
            {
                Console.WriteLine($"\nREGISTRACIJA NEUSPJESNA");
            }
            else
            {
                Console.WriteLine("\nUspjesno registrovan, cekam porudzbine");


                var managerRepo = new ManagerRepository(2);
                var managerManagementService = new ManagerManagementService(managerRepo);
                new Thread(() =>
                {
                    managerManagementService.TakeOrReserveATable(managerNumber, Domain.Enums.ClientType.Manager);
                }).Start(); //IsBackground = true ovo ne treba ovde jer sa zatvori klijent onda

                var releaseATableManagerService = new ReleaseATableManagerService(managerRepo, 4001);
                releaseATableManagerService.ReleaseATable(managerNumber); //jako je bitno da ovde i gore bude managerNumber a ne clientId

                //Console.ReadKey();
            }
        }
    }
}
