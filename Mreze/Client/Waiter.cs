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
                Console.WriteLine("Upotreba: Client <WaiterId> <TotalWaiters> <UdpPort>");
                return;
            }

            int waiterId = int.Parse(args[0]);
            int totalWaiters = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"Waiter #{waiterId} (od {totalWaiters}), UDP port {udpPort}");

            // 1) Postavljanje repoa i servisa za naručivanje i sto
            var waiterRepo = new WaiterRepository(totalWaiters);
            var orderService = new MakeAnOrderWaiterService(waiterRepo);
            var tableService = new TakeATableClientService(orderService, waiterRepo, udpPort);
            var waiterMgmt = new WaiterManagementService(tableService, waiterRepo, udpPort + 2000, orderService);

            // 2) Pokrećemo jedan Thread koji obavlja:
            //    a) TCP REGISTER
            //    b) Čeka ACK
            //    c) Beskonačno čita READY;… poruke
            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;

            new Thread(() =>
            {
                try
                {
                    // a) Kreiramo TCP socket i povežemo se
                    var sock = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));

                    // b) Pošaljemo REGISTER;{waiterId};Waiter;{udpPort}\n
                    string regMsg = $"REGISTER;{waiterId};Waiter;{udpPort}\n";
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
                    }

                    // d) Beskonačna petlja za READY;{tableNo};{waiterId}\n

                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int r = sock.Receive(buffer);

                        string line = Encoding.UTF8.GetString(buffer).Trim();

                        if (line.StartsWith("READY;"))
                        {
                            var parts = line.Split(';');
                            int tableNo = int.Parse(parts[1]);
                            Console.WriteLine($"Porudžbina za sto {tableNo} je spremna! Nosim…");
                            Thread.Sleep(1500);
                            Console.WriteLine($"Porudžbina za sto {tableNo} je dostavljena.");
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

            // 4) UDP Socket listener for MAKE_ORDER
            /*
            new Thread(() =>
            {
                try
                {
                    int listenPort = udpPort + 100; // UDP port to listen on
                    Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpSocket.Bind(new IPEndPoint(IPAddress.Any, listenPort));

                    Console.WriteLine($"[UDP Listener] Waiting for MAKE_ORDER messages on port {listenPort}...");

                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        int received = udpSocket.ReceiveFrom(buffer, ref remoteEP);
                        string message = Encoding.UTF8.GetString(buffer, 0, received);

                        if (message.StartsWith("MAKE_ORDER:"))
                        {
                            var parts = message.Split(':');
                            if (parts.Length >= 3 &&
                                int.TryParse(parts[1], out int tableNo) &&
                                int.TryParse(parts[2], out int guestCount))
                            {
                                waiterRepo.SetWaiterState(waiterId, true);
                                lock (consoleLock)
                                {
                                    Console.WriteLine($"[UDP Listener] MAKE_ORDER received for table {tableNo} with {guestCount} guests.");

                                    // Call your order logic here
                                    orderService.MakeAnOrder(tableNo, guestCount, waiterId);
                                }
                                waiterRepo.SetWaiterState(waiterId, false);
                            }
                            else
                            {
                                Console.WriteLine("[UDP Listener] Invalid MAKE_ORDER message format.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[UDP Listener] Unknown message received: {message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UDP Listener ERROR] {ex.Message}");
                }
            })
            { IsBackground = true }.Start();
            */

            // 3) Glavna nit: interaktivni meni za uzimanje/rezervisanje stola
            //waiterMgmt.TakeOrReserveATable(waiterId, Domain.Enums.ClientType.Waiter);

            Thread waiterThread = new Thread(() => waiterMgmt.TakeOrReserveATable(waiterId, Domain.Enums.ClientType.Waiter));
            waiterThread.Start();

            //Console.WriteLine("Konobar je zatvoren. Pritisnite ENTER za kraj...");
            //Console.ReadLine();
        }
    }
}
