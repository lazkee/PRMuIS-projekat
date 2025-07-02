using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Domain.Repositories.WaiterRepository;
using Services.MakeAnOrderServices;
using Services.TakeATableServices;
using Services.WaiterManagementServices;
namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            // 1) Parsiranje argumenata
            Console.WriteLine($"Waiter number #{args[1]},");
            Console.WriteLine($"clientId #{args[0]}");
            Console.WriteLine($"Port #{args[2]}");

            int.TryParse(args[0], out int waiterId);
            int.TryParse(args[1], out int numberOfWaiters);
            int.TryParse(args[2], out int udpPort);

             // 2) Inicijalizacija repozitorijuma i servisa
            var waiterRepo = new WaiterRepository(numberOfWaiters);
            var orderService = new MakeAnOrderWaiterService(waiterRepo);
             // ZA SERVER: on sluša UDP na portu 4000
            const int serverUdpPort = 4000;
            var tableService = new TakeATableClientService(orderService, waiterRepo,serverUdpPort);

            // **OVDE JE KLJUČ** – prosledi waiterRepo i menadžeru
            var waiterMgmt = new WaiterManagementService(tableService, waiterRepo);

            // 3) Pokretanje background-threada koji sluša READY;… poruke
            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;
            new Thread(() =>
            {
                using (var tcp = new TcpClient(serverIp, serverPort))
                using (var reader = new StreamReader(tcp.GetStream(), Encoding.UTF8))
                using (var writer = new StreamWriter(tcp.GetStream(), Encoding.UTF8) { AutoFlush = true })
                {
                    // Registracija za notifikacije
                    writer.WriteLine($"REGISTER;{waiterId};Waiter;{udpPort}");
                    if (reader.ReadLine() != "REGISTERED") return;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("READY;")) continue;
                        // READY;{table};{waiterId}
                        var parts = line.Split(';');
                        int tableNum = int.Parse(parts[1]);
                        int wId = int.Parse(parts[2]);

                        // signaliziraj menadžeru da je porudžbina spremna
                        waiterRepo.SetOrderReady(wId);
                        Console.WriteLine($"Porudzbina za sto {tableNum}je spremna! Nosim je…");
                        Thread.Sleep(1500);
                        Console.WriteLine("Porudzbina odnesena!");
                        waiterRepo.ClearOrderReady(wId);

                    }
                }
            })
            { IsBackground = true }.Start();

            // 4) Pokreni glavni meni konobara
            waiterMgmt.WaiterIsServing(waiterId);
        }
    }
}
