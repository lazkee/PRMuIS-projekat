using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Domain.Repositories.WaiterRepository;
using Services.MakeAnOrderServices;
using Services.TakeATableClientServices;
using Services.WaiterManagementServices;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Upotreba: Client <WaiterId> <TotalWaiters> <UdpPort>");
                return;
            }

            int waiterId = int.Parse(args[0]);
            int totalWaiters = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"Waiter number #{totalWaiters}, clientId #{waiterId}, UDP port #{udpPort}");

            // 1) Inicijalizacija repozitorijuma i servisa
            var waiterRepo = new WaiterRepository(totalWaiters);
            
            var orderService = new MakeAnOrderWaiterService(waiterRepo);
            var tableService = new TakeATableClientService(orderService, waiterRepo, udpPort);
            var waiterMgmt = new WaiterManagementService(tableService, waiterRepo);

            // 2) Startujemo background thread za READY-notifikacije (TCP)
            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;
            new Thread(() =>
            {
                try
                {
                    var tcp = new TcpClient(serverIp, serverPort);
                    var stream = tcp.GetStream();
                    var reader = new StreamReader(stream, Encoding.UTF8);
                    var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    // Registracija za notifikacije, šaljemo i isti udpPort (iako ga server za Waiter ne koristi za PREPARE)
                    writer.WriteLine($"REGISTER;{waiterId};Waiter;{udpPort}");
                    if (reader.ReadLine() != "REGISTERED")
                        return;

                    Console.WriteLine("Notification listener pokrenut, čekam READY poruke...");

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("READY;"))
                            continue;

                        // READY;{tableNo};{waiterId}
                        var parts = line.Split(';');
                        int tableNo = int.Parse(parts[1]);

                        Console.WriteLine($"Porudžbina za sto {tableNo} je spremna! Nosim je…");
                        Thread.Sleep(1500);
                        Console.WriteLine($"Porudžbina za sto {tableNo} je dostavljena.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationThread ERROR] {ex.Message}");
                }
            })
            { IsBackground = true }
            .Start();

            // 3) Glavna nit: meni koji ne završava dok god ne odaberemo 0
            waiterMgmt.TakeOrReserveATable(waiterId, Domain.Enums.ClientType.Waiter);

            // 4) Kad izađemo iz menija (opcija 0), možemo da blokiramo još malo
            Console.WriteLine("Konobar je zatvoren. Pritisni ENTER za kraj...");
            Console.ReadLine();
        }
    }
}
