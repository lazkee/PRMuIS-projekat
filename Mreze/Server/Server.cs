using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Repositories.ManagerRepository;
using Domain.Repositories.OrderRepository;
using Domain.Repositories.WaiterRepository;
using Domain.Services;
using Infrastructure.Networking;
using Services.NotificationServices;
using Services.ReleaseATableServices;
using Services.SendOrderForPreparationServices;
using Services.ServerServices;
using Services.TakeATableServices;
using Domain.Helpers;
using Domain.Repositories.TableRepository;
using System.Linq;
namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server je pokrenut. Pritisni ENTER za zaustavljanje.");
            //  Inicijalizacija repozitorijuma i servisa
            IClientDirectory clientDirectory = new ClientDirectory();
            var notificationService = new NotificationService(clientDirectory);

            IOrderRepository foodRepo = new FoodOrderRepository();
            IOrderRepository drinkRepo = new DrinkOrderRepository();
            var prepService = new SendOrderForPreparationService(
                clientDirectory,
                foodRepo,
                drinkRepo);
            // TCP listener za registraciju i notifikacije 
            new Thread(() => StartClientListener(
                clientDirectory,
                notificationService,
                registerPort: 5000,
                readyPort: 5001
                ))
            {
                IsBackground = true
            }.Start();
            Console.WriteLine("[Server] TCP ClientListener pokrenut na portu 5000.");

            // UDP listener za raspodelu stolova
            var readService = new ServerReadTablesService();
            var managerRepository = new ManagerRepository(1);       
            var tableService = new TakeATableServerService(readService, managerRepository, listenPort: 4000);
            new Thread(tableService.TakeATable) { IsBackground = true }.Start();
            Console.WriteLine("[Server] UDP TableListener pokrenut na portu 4000.");

            CalculateTheBill kasa = new CalculateTheBill();
            new Thread(() => StartBillListener(kasa)) { IsBackground = true }.Start();
            // UDP listener za otkazivanje rezervacija koje su istekle na portu 4001 (menadzer salje serveru da je rezervacija otkazana - istekla, ako u roku od 10 sekundi ne dodju gosti)

            var releaseATableService = new ReleaseATableServerService(readService, 4001);
            releaseATableService.ReleaseATable();   //Listener thread se nalazi u servisu (ne pravi se u Serveru). Nema neke razlike ali ovako je mozda cistije
            //I da napravimo jos 2 servisa za ove dole TCP sto su nam static
            Console.WriteLine("[Server] UDP TableCancelationListener pokrenut na portu 4001.");


            // UDP listener kada konobar zatrazuje proveru rezervacije
            new Thread(() => ReservationVerificationServer.Start(managerRepository))
            {
                IsBackground = true
            }.Start();

            Console.WriteLine("[Server] UDP ReservationVerificationServer started on port 4003.");

            //  TCP listener za porudžbine na portu 15000
           new Thread(() => StartOrderDeliveredListener()) { IsBackground = true}.Start();
            new Thread(() => StartOrderListener(prepService, 15000)) { IsBackground = true }.Start();
            Console.WriteLine("[Server] TCP OrderListener pokrenut na portu 15000.");

            //Pokretanje i registrovanje klijentskih procesa
            var createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, ClientType.Waiter);
            createClientInstance.BrojITipKlijenta(1, ClientType.Cook);
            createClientInstance.BrojITipKlijenta(1, ClientType.Bartender);
            createClientInstance.BrojITipKlijenta(1, ClientType.Manager);
            Console.ReadLine();
        }

        public static class ReservationVerificationServer
        {
            public static void Start(IManagerRepository managerRepo)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                   
                    EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 4003);
                    socket.Bind(localEndPoint);

                    Console.WriteLine("[ReservationServer] UDP Server za rezervacije je pokrenut na portu 4003...");


                    while (true)
                    {
                        try
                        {
                            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] buffer = new byte[1024];

                            int received = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                            if (received <= 0) continue;

                            string message = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                            Console.WriteLine($"[ReservationServer] Primljena poruka: {message}");

                            if (int.TryParse(message, out int reservationCode))
                            {
                                bool isValid = managerRepo.CheckReservation(reservationCode);
                                int tableNumber = 0;

                                if (isValid)
                                {
                                    tableNumber = managerRepo.GetTableNumber(reservationCode);
                                    managerRepo.RemoveReservation(reservationCode);
                                    Console.WriteLine($"[ReservationServer] Rezervacija validna #{reservationCode}, dodijeljen sto {tableNumber}");
                                    Console.WriteLine($"[Server] Sto broj {tableNumber} je zauzet");

                                    try
                                    {
                                        string managerMessage = $"RESERVATION_USED;{reservationCode}";
                                        byte[] managerData = Encoding.UTF8.GetBytes(managerMessage);

                                        Socket managerNotifySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                        IPEndPoint managerEndpoint = new IPEndPoint(IPAddress.Loopback, 4010); 

                                        managerNotifySocket.SendTo(managerData, managerEndpoint);
                                        managerNotifySocket.Close();

                                        Console.WriteLine($"[ReservationServer] Poslata poruka menadžeru: {managerMessage}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[ReservationServer] Greška prilikom slanja menadžeru: {ex.Message}");
                                    }

                                }
                                else
                                {
                                    Console.WriteLine($"[ReservationServer] Netacan ili vec iskoristen broj rezervacije: {reservationCode}");
                                }

                                string response = isValid
                                    ? $"OK;{tableNumber}"
                                    : "ERROR;Invalid reservation";

                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                                socket.SendTo(responseBytes, remoteEndPoint);
                            }
                            else
                            {
                                Console.WriteLine("[ReservationServer] Primljen nepoznat tip poruke.");
                            }
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine($"[ReservationServer] SocketException: {se.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ReservationServer] General exception: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReservationServer] Neuspjesno pokretanje servisa: {ex.Message}");
                }
            }
        }


        private static void StartGuestsArrivedListener(int port, IWaiterRepository waiterRepo, IClientDirectory clientDirectory)
        {
            var udpClient = new UdpClient(port);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine($"[Server] Čeka na GUESTS_ARRIVED poruke na portu {port}...");

            while (true)
            {
                try
                {
                    var data = udpClient.Receive(ref remoteEP);
                    var message = Encoding.UTF8.GetString(data);

                    if (message.StartsWith("GUESTS_ARRIVED:"))
                    {
                        var parts = message.Split(':');
                        if (parts.Length == 3 && int.TryParse(parts[1], out int reservationNumber) && int.TryParse(parts[2], out int tableNumber))
                        {

                            Console.WriteLine($"[Server] Gosti su stigli za rezervaciju #{reservationNumber} i broj stola {tableNumber}.");


                            int freeWaiterId = -1;
                            foreach (var kvp in waiterRepo.GetAllWaiterStates())
                            {
                                if (!kvp.Value)
                                {
                                    freeWaiterId = kvp.Key;
                                    break;
                                }
                            }

                            if (freeWaiterId == -1)
                            {
                                Console.WriteLine("[Server] Nema slobodnih konobara trenutno.");
                            }
                            else
                            {
                                waiterRepo.SetWaiterState(freeWaiterId, true);
                                Console.WriteLine($"[Server] Konobar #{freeWaiterId} dodeljen za rezervaciju #{reservationNumber}.");

                              
                                int brojStola = 1;

                                int brojGostiju = 4;
                                string waiterMessage = $"MAKE_ORDER:{brojStola}:{brojGostiju}:{tableNumber}";
                                byte[] data1 = Encoding.UTF8.GetBytes(waiterMessage);

                                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                IPEndPoint waiterEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6100 + freeWaiterId - 1);

                                socket.SendTo(data1, waiterEndPoint);
                                socket.Close();

                                Console.WriteLine($"[Server] Poslata poruka konobaru #{freeWaiterId} na port {waiterEndPoint.Port}: {waiterMessage}");

                            }


                        }
                        else
                        {
                            Console.WriteLine($"[Server] Nevalidan broj rezervacije: {message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Server] Nepoznata poruka na portu 4002: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] GuestsArrivedListener: {ex.Message}");
                }
            }
        }

        private static void StartBillListener(CalculateTheBill kasa)
        {
            var listenSock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            listenSock.Bind(new IPEndPoint(IPAddress.Any, 5003));
            listenSock.Listen(100);
            Console.WriteLine($"[Server] Čeka zahteve za racun na portu 5003");

            while (true)
            {
                // Prihvatamo novu konekciju od klijenta
                var client = listenSock.Accept();
                client.Blocking = true;

                new Thread(() =>
                {
                    try
                    {
                        var buf = new byte[8192];
                        int n;

                        //  U petlji primamo i obrađujemo sve poruke na ovoj konekciji
                        while ((n = client.Receive(buf)) > 0)
                        {
                            var line = Encoding.UTF8.GetString(buf, 0, n).Trim();
                            var parts = line.Split(';');

                            if (parts[0] == "KUSUR")
                            {
                                int br = int.Parse(parts[1]);
                                int iznos = int.Parse(parts[2]);
                                int uplaceno = int.Parse(parts[3]);
                                string poruka = $"[Naplata sto #{br}] Ukupno za uplatu (napojnica uračunata) {iznos} Uplaćeno: {uplaceno} Vraćen kusur: {uplaceno - iznos}";

                                Console.WriteLine($"[Server] {poruka}");
                                // Pošaljemo odgovor natrag klijentu
                                client.Send(Encoding.UTF8.GetBytes(poruka + "\n"));


                                // Očistimo sto

                                TableRepository.ClearTable(br);
                                Console.WriteLine($"[Server] Sto broj {br} je sada slobodan");
                            }
                            else if (parts[0] == "RACUN")
                            {
                                int brStola = int.Parse(parts[1]);
                                Console.WriteLine($"[Server] Sto broj {brStola} je zatražio račun");

                                string odgovor = kasa.Calculate(brStola);
                                client.Send(Encoding.UTF8.GetBytes(odgovor + "\n"));
                            }
                            else
                            {
                                Console.WriteLine($"[Server] Neočekivana poruka: {line}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] BillListener: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    }
                    finally
                    {
                        // 3) Zatvorimo klijentski soket kada nema više podataka
                        client.Close();
                    }
                })
                { IsBackground = true }.Start();
            }
        }


        private static void StartClientListener(
        IClientDirectory directory,
        NotificationService notifier,
        int registerPort,
        int readyPort
         )
        {
            // 1) Kreiramo dva ne‑blokirajuća TCP soketa: jedan za REGISTER, jedan za READY
            var registerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            registerSock.Bind(new IPEndPoint(IPAddress.Any, registerPort));
            registerSock.Listen(100);
            registerSock.Blocking = false;

            var readySock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            readySock.Bind(new IPEndPoint(IPAddress.Any, readyPort));
            readySock.Listen(100);
            readySock.Blocking = false;

            Console.WriteLine($"[Server] Socket za registraciju na portu {registerPort}");
            Console.WriteLine($"[Server] Socket za obavjestenja o gotovim porudzbinama na portu {readyPort}");

            try
            {
                while (true)
                {
                    var readList = new List<Socket> { registerSock, readySock };
                    var errorList = new List<Socket> { registerSock, readySock };

                    // Select čeka do 1 sekunde na događaje za čitanje ili greške
                    Socket.Select(readList, null, errorList, 1000);

                    //  Obrada svakog soketa koji je spreman
                    foreach (var s in readList)
                    {
                        if (s == registerSock)
                        {
                            var client = registerSock.Accept();
                            client.Blocking = true;

                            new Thread(() =>
                            {
                                try
                                {
                                    var buffer = new byte[8192];
                                    int bytesReceived;
                                    // petlja–prima više REGISTER poruka u istoj konekciji
                                    while ((bytesReceived = client.Receive(buffer)) > 0)
                                    {
                                        var line = Encoding.UTF8.GetString(buffer, 0, bytesReceived).Trim();
                                        var parts = line.Split(';');
                                        if (parts.Length == 4 && parts[0] == "REGISTER")
                                        {
                                            int id = int.Parse(parts[1]);
                                            var type = (ClientType)Enum.Parse(typeof(ClientType), parts[2], true);
                                            int udpPort = int.Parse(parts[3]);
                                            var ip = ((IPEndPoint)client.RemoteEndPoint).Address;

                                            directory.Register(new ClientInfo
                                            {
                                                Id = id,
                                                Type = type,
                                                Socket = client,
                                                Endpoint = new IPEndPoint(ip, udpPort)
                                            });

                                            var ok = Encoding.UTF8.GetBytes("REGISTERED\n");
                                            client.Send(ok);
                                            Console.WriteLine($"[Server] Registrovan klijent: ID={id}, Tip={type}, UDPport={udpPort}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"[Server] Neuspješna registracija [{line}]");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] StartClientListener (REGISTER): {ex.Message}");
                                }
                                finally
                                {
                                    client.Close();
                                }
                            })
                            { IsBackground = true }.Start();
                        }
                        else if (s == readySock)
                        {
                            var client = readySock.Accept();
                            client.Blocking = true;

                            new Thread(() =>
                            {
                                try
                                {
                                    var buffer = new byte[8192];
                                    int bytesReceived;
                                    // petlja–prima više READY poruka u istoj konekciji
                                    while ((bytesReceived = client.Receive(buffer)) > 0)
                                    {
                                        var line = Encoding.UTF8.GetString(buffer, 0, bytesReceived).Trim();
                                        var parts = line.Split(';');
                                        if (parts.Length == 4 && parts[0] == "READY")
                                        {
                                            int tableId = int.Parse(parts[1]);
                                            int waiterId = int.Parse(parts[2]);
                                            string tipPorudzb = parts[3];

                                            Console.WriteLine(
                                              $"[Server] Porudzbina {tipPorudzb} za konobara #{waiterId} za sto {tableId} je gotova");
                                            notifier.NotifyOrderReady(tableId, waiterId, tipPorudzb);
                                            ArticleCategory tip = ArticleCategory.PICE;
                                            if (tipPorudzb == "hrane") { tip = ArticleCategory.HRANA; }
                                            List<Order> orders = TableRepository.GetByID(tableId).TableOrders.Where(o => o.ArticleCategory == tip).ToList();
                                            foreach (Order o in orders)
                                            {
                                                o._articleStatus = ArticleStatus.SPREMNO;
                                                Console.WriteLine(o.ToString());
                                                
                                            }
                                            

                                        }
                                        else
                                        {
                                            Console.WriteLine($"[Server] Neočekivana poruka na READY socket: {line}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] StartClientListener (READY): {ex.Message}");
                                }
                                finally
                                {
                                    client.Close();
                                }
                            })
                            { IsBackground = true }.Start();
                        }
                    }

                    //  Obrada grešaka na soketima
                    if (errorList.Count > 0)
                    {
                        Console.WriteLine($"[Server] Detektovano {errorList.Count} grešaka na soketima, zatvaram ih...");
                        foreach (var s in errorList)
                        {
                            Console.WriteLine($"[Server] Zatvaram socket: {s.LocalEndPoint}");
                            s.Close();
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[ERROR] StartClientListener: {ex.Message}");
            }
        }



        private static void StartOrderDeliveredListener()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 4011));
            socket.Listen(100);
            Console.WriteLine($"[Server] Čeka poruke o dostavljenoj porudzbini na TCP portu {4011}...");

            while (true)
            {
                Socket client = socket.Accept();
                new Thread(() =>
                {
                    try
                    {
                        var buffer = new byte[8192];
                        int bytesReceived;
                        while ((bytesReceived = client.Receive(buffer)) > 0)
                        {
                            if (bytesReceived <= 0) return;

                            var line = Encoding.UTF8
                                .GetString(buffer, 0, bytesReceived)
                                .Trim();
                            string[] parts = line.Split(';');
                            if (parts[0] == "DELIVERED")
                            {
                                ArticleCategory tip = ArticleCategory.PICE;
                                if (parts[2] == "hrane") { tip = ArticleCategory.HRANA; }
                                
                                int brStola = int.Parse(parts[1]);
                                List<Order> orders = TableRepository.GetByID(brStola).TableOrders.Where(o => o.ArticleCategory == tip).ToList();
                                Console.WriteLine($"[Server] Porudzbina za sto {brStola} je dostavljena");
                                foreach (Order o in orders)
                                {
                                    o._articleStatus = ArticleStatus.ISPORUCENO;
                                    Console.WriteLine(o.ToString());
                                }

                            }

                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"{ex.Message} {ex.StackTrace}"); }
                    finally { client.Close(); }

                }){ IsBackground = true }
                .Start();
            }   }
        private static void StartOrderListener(
                ISendOrderForPreparation prepService,
                int tcpPort)
        {
            
            var orderSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            orderSocket.Bind(new IPEndPoint(IPAddress.Any, tcpPort));
            orderSocket.Listen(10);  
            Console.WriteLine($"[Server] Čeka ORDER poruke na TCP portu {tcpPort}...");


            while (true)
            {
                
                var client = orderSocket.Accept();

                new Thread(() =>
                {
                    try
                    {
                        
                        var buffer = new byte[8192];
                        int bytesReceived;
                        while ((bytesReceived = client.Receive(buffer)) > 0)
                        {
                            if (bytesReceived <= 0) return;

                            
                            var line = Encoding.UTF8
                                .GetString(buffer, 0, bytesReceived)
                                .Trim();  

                            
                            // ORDER;{waiterId};{tableNumber};{base64…}
                            var parts = line.Split(new[] { ';' }, 4);
                            if (parts.Length != 4 || parts[0] != "ORDER")
                            {
                                Console.WriteLine($"[Server] Neispravna poruka: {line}");
                                return;
                            }

                            int waiterId = int.Parse(parts[1]);
                            int tableNumber = int.Parse(parts[2]);
                            string b64 = parts[3];

                            
                            byte[] tableData = Convert.FromBase64String(b64);

                            // Deserijalizacija
                            Domain.Models.Table table;
                            using (var ms = new MemoryStream(tableData))
                            {
                                var bf = new BinaryFormatter();
                                table = (Domain.Models.Table)bf.Deserialize(ms);
                            }


                            TableRepository.UpdateTable(table);
                            Console.WriteLine(
                                $"[Server] Porudzbina od konobara {waiterId} za sto {table.TableNumber}, " +
                                $"{table.TableOrders.Count} stavki.");
                            Thread.Sleep(1000);
                            foreach (Order o in TableRepository.GetByID(tableNumber).TableOrders)
                            {
                                o._articleStatus = ArticleStatus.PRIPREMA;
                                Console.WriteLine(o.ToString());
                            }


                            //  Prosledjujemo u servis
                            prepService.SendOrder(
                                waiterId,
                                table.TableNumber,
                                table.TableOrders);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] OrderListener: {ex.GetType().Name}: {ex.Message}");
                    }
                    finally
                    {
                        client.Close();
                    }
                })
                { IsBackground = true }
                .Start();
            }
        }
    }
}

