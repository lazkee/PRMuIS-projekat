using System;
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
        private static readonly object consoleLock = new object();
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Greska pri pokretanju konobara");
                return;
            }

            int waiterId = int.Parse(args[0]);
            int totalWaiters = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"Konobar id={waiterId},  port {udpPort}");

            // Postavljanje repoa i servisa za naručivanje i sto
            var waiterRepo = new WaiterRepository(totalWaiters);
            var orderService = new MakeAnOrderWaiterService(waiterRepo, udpPort);
            var tableService = new TakeATableClientService(orderService, waiterRepo, udpPort);
            var waiterMgmt = new WaiterManagementService(tableService, waiterRepo, udpPort + 2000, orderService);

            // Jedan Thread koji obavlja:
            //    a) TCP REGISTER
            //    b) Čeka ACK
            //    c) Beskonačno čita READY;… poruke
            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;

            new Thread(() =>
            {
                try
                {
                    //  Kreiramo TCP socket i povežemo se
                    Socket sock = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));

                    //kreiramo drugi tcp socket za obavjestenje o isporucenoj porudzbini
                    Socket orderSock = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    orderSock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), 4011));

                    // Pošaljemo REGISTER;{waiterId};Waiter;{udpPort}\n
                    string regMsg = $"REGISTER;{waiterId};Waiter;{udpPort}\n";
                    sock.Send(Encoding.UTF8.GetBytes(regMsg));

                    // Prihvatimo ACK liniju “REGISTERED\n”
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
                        Console.WriteLine("\nUSPJESNO REGISTROVAN");
                    }

                    // Beskonacna petlja za READY;{tableNo};{waiterId}\n

                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int r = sock.Receive(buffer);

                        string line = Encoding.UTF8.GetString(buffer).Trim();

                        if (line.StartsWith("READY;"))
                        {
                            var parts = line.Split(';');
                            int tableNo = int.Parse(parts[1]);
                            string tipPorudzbine = parts[2];
                            Console.WriteLine($"Porudžbina za sto {tableNo} je spremna! Nosim…");
                            Thread.Sleep(1500);
                            Console.WriteLine($"Porudžbina {tipPorudzbine} za sto {tableNo} je dostavljena.");
                            orderSock.Send(Encoding.UTF8.GetBytes($"DELIVERED;{tableNo};{tipPorudzbine}"));

                        }

                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationThread ERROR] {ex.Message}");
                }
            })
            { IsBackground = true }
            .Start();

            

            //  Glavna nit: interaktivni meni za uzimanje/rezervisanje stola
            

            Thread waiterThread = new Thread(() => waiterMgmt.TakeOrReserveATable(waiterId, Domain.Enums.ClientType.Waiter));
            waiterThread.Start();
            
            
        }
    }
}
