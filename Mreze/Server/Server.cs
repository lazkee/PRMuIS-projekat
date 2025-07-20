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

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            // 1) Inicijalizacija repozitorijuma i servisa
            IClientDirectory clientDirectory = new ClientDirectory();
            var notificationService = new NotificationService(clientDirectory);

            IOrderRepository foodRepo = new FoodOrderRepository();
            IOrderRepository drinkRepo = new DrinkOrderRepository();
            var prepService = new SendOrderForPreparationService(
                clientDirectory,
                foodRepo,
                drinkRepo);
            // 3.3) TCP listener za registraciju i notifikacije na portu 5000
            new Thread(() => StartClientListener(
                clientDirectory,
                notificationService,
                registerPort: 5000,
                readyPort: 5001,
                billPort: 5003
                ))
            {
                IsBackground = true
            }.Start();
            Console.WriteLine("[Server] TCP ClientListener pokrenut na portu 5000.");

            // 3.1) UDP listener za raspodelu stolova
            var readService = new ServerReadTablesService();
            var managerRepository = new ManagerRepository(1);       //nebitan broj, samo se stolovi koriste
            var tableService = new TakeATableServerService(readService, managerRepository, listenPort: 4000);
            new Thread(tableService.TakeATable) { IsBackground = true }.Start();
            Console.WriteLine("[Server] UDP TableListener pokrenut na portu 4000.");

            // 3.11) UDP listener za otkazivanje rezervacija koje su istekle na portu 4001 (menadzer salje serveru da je rezervacija otkazana - istekla, ako u roku od 10 sekundi ne dodju gosti)

            var releaseATableService = new ReleaseATableServerService(readService, 4001);
            releaseATableService.ReleaseATable();   //Listener thread se nalazi u servisu (ne pravi se u Serveru). Nema neke razlike ali ovako je mozda cistije
            //I da napravimo jos 2 servisa za ove dole TCP sto su nam static
            Console.WriteLine("[Server] UDP TableCancelationListener pokrenut na portu 4001.");

            // 3.12) bice UDP listener za goste koji su stigli sa rezervacijom na portu 4002 (menadzer salje serveru da su stigli)
            /*
            IWaiterRepository waiterRepo = new WaiterRepository(3);

            new Thread(() => StartGuestsArrivedListener(4002, waiterRepo, clientDirectory)) { IsBackground = true }.Start();
            Console.WriteLine("[Server] GuestsArrivedListener pokrenut na portu 4002.");
            //ne radimo ipak na ovaj nacin*/

            // 3.13) UDP listener kada konobar zatrazuje proveru rezervacije

            //var reservationServer = new WaiterReservationValidationServerService(managerRepository);
            new Thread(() => ReservationVerificationServer.Start(managerRepository))
            {
                IsBackground = true
            }.Start();

            Console.WriteLine("[Server] UDP ReservationVerificationServer started on port 4003.");

            // 3.2) TCP listener za porudžbine na portu 15000
            //new Thread(() => StartOrderListener(prepService, tcpPort: 15000)) { IsBackground = true }
            //    .Start();
            //Console.WriteLine("[Server] TCP OrderListener pokrenut na portu 15000.");
            new Thread(() => StartOrderListenerUdp(prepService, 15000)) { IsBackground = true }.Start();
            Console.WriteLine("[Server] UDP OrderListener pokrenut na portu 15000.");

            // 4) Držimo main nit živom
            Console.WriteLine("Server je pokrenut. Pritisni ENTER za zaustavljanje.");

            // 2) Pokretanje i registrovanje klijentskih procesa
            var createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, ClientType.Waiter);
            createClientInstance.BrojITipKlijenta(1, ClientType.Cook);
            createClientInstance.BrojITipKlijenta(1, ClientType.Bartender);
            createClientInstance.BrojITipKlijenta(1, ClientType.Manager);
            Console.ReadLine();
        }

        /// <summary>
        /// Listener koji prima binarni protokol: length-prefixed waiterId i serijalizovani Table.
        /// Deserijalizuje ih, loguje kao Base64 i potom prosleđuje prepService.SendOrder.
        /// gornji thread pravi novi thread, da on moze da obavlja posao bez da blokira ostale
        /// </summary>
        /// 

        /*public static class ReservationVerificationServer
        {
            public static void Start(IManagerRepository managerRepo)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    EndPoint localEndPoint = new IPEndPoint(IPAddress.Loopback, 4003);
                    socket.Bind(localEndPoint);
                    Console.WriteLine("UDP Reservation Server is running on port 4003...");

                    //EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    //byte[] buffer = new byte[1024];


                    while (true)
                    {
                        try
                        {
                            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] buffer = new byte[1024];

                            int received = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                            if (received <= 0)
                                continue;

                            string message = Encoding.UTF8.GetString(buffer, 0, received);

                            if (int.TryParse(message, out int reservationCode))
                            {
                                Console.WriteLine($"[ReservationServer] Received reservation code: {reservationCode}");

                                bool isValid = managerRepo.CheckReservation(reservationCode);
                                int tableNumber = 0;
                                if (isValid)
                                {
                                    tableNumber = managerRepo.GetTableNumber(reservationCode);
                                    managerRepo.RemoveReservation(reservationCode);
                                }

                                string response = isValid
                                    ? $"OK;{tableNumber}"
                                    : "ERROR;Invalid reservation";

                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                                socket.SendTo(responseBytes, remoteEndPoint);
                            }
                            else
                            {
                                Console.WriteLine("[ReservationServer] Invalid message received.");
                            }
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine($"[ReservationServer] Socket exception: {se.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ReservationServer] Unexpected error: {ex}");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ReservationServer] Error: {ex.Message}");
                }
            }
        }*/

        public static class ReservationVerificationServer
        {
            public static void Start(IManagerRepository managerRepo)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    // Koristi IPAddress.Any umesto Loopback da podrži sve lokalne konekcije
                    EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 4003);
                    socket.Bind(localEndPoint);

                    Console.WriteLine("[ReservationServer] UDP Reservation Server is running on port 4003...");

                    // Opcionalno: dodaj timeout da se ReceiveFrom ne blokira zauvek
                    // socket.ReceiveTimeout = 10000;

                    while (true)
                    {
                        try
                        {
                            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            byte[] buffer = new byte[1024];

                            int received = socket.ReceiveFrom(buffer, ref remoteEndPoint);
                            if (received <= 0) continue;

                            string message = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                            Console.WriteLine($"[ReservationServer] Received message: {message}");

                            if (int.TryParse(message, out int reservationCode))
                            {
                                bool isValid = managerRepo.CheckReservation(reservationCode);
                                int tableNumber = 0;

                                if (isValid)
                                {
                                    tableNumber = managerRepo.GetTableNumber(reservationCode);
                                    managerRepo.RemoveReservation(reservationCode);
                                    Console.WriteLine($"[ReservationServer] Valid reservation #{reservationCode}, assigned table {tableNumber}");

                                    // Posalji obavestenje menadzeru na port 4010
                                    try
                                    {
                                        string managerMessage = $"RESERVATION_USED;{reservationCode}";
                                        byte[] managerData = Encoding.UTF8.GetBytes(managerMessage);

                                        Socket managerNotifySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                                        IPEndPoint managerEndpoint = new IPEndPoint(IPAddress.Loopback, 4010); // ili IP menadzera ako nije lokalno

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
                                    Console.WriteLine($"[ReservationServer] Invalid or used reservation code: {reservationCode}");
                                }

                                string response = isValid
                                    ? $"OK;{tableNumber}"
                                    : "ERROR;Invalid reservation";

                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                                socket.SendTo(responseBytes, remoteEndPoint);
                            }
                            else
                            {
                                Console.WriteLine("[ReservationServer] Malformed message received.");
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
                    Console.WriteLine($"[ReservationServer] Failed to start listener: {ex.Message}");
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



                            // Ovde možeš dodati logiku za označavanje da su gosti stigli
                            // ili ažurirati bazu / stanje servera / osloboditi timer itd.

                            int freeWaiterId = -1;
                            foreach (var kvp in waiterRepo.GetAllWaiterStates())
                            {
                                if (!kvp.Value) // means waiter is FREE
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

                                // Dummy data for this example — should be fetched from reservation
                                //int brojStola = reservationNumber; // or from reservation data
                                //ITableRepository tableRepo = new TableRepository();
                                int brojStola = 1;

                                //var waiter = clientDirectory.GetById(freeWaiterId);
                                //IPEndPoint waiterEndPoint = waiter.Endpoint;
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

        private static void StartOrderListenerUdp(
            ISendOrderForPreparation prepService,
             int udpPort)
        {
            var udp = new UdpClient(udpPort);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            Console.WriteLine($"[Server] Čeka UDP ORDER na portu {udpPort}...");

            while (true)
            {
                try
                {
                    // 1) Receive entire datagram
                    var data = udp.Receive(ref remoteEP);
                    var text = Encoding.UTF8.GetString(data).Trim();
                    // očekujemo npr. "ORDER;1;3;QmFzZTY0..."
                    if (!text.StartsWith("ORDER;")) continue;

                    // 2) Parsiraj
                    var parts = text.Split(new[] { ';' }, 4);
                    int waiterId = int.Parse(parts[1]);
                    int tableNumber = int.Parse(parts[2]);
                    string b64 = parts[3];

                    Console.WriteLine($"[Server] UDP ORDER od konobara {waiterId} za sto {tableNumber}");

                    // 3) Deserijalizuj Table iz Base64
                    var tblBytes = Convert.FromBase64String(b64);
                    Table table;
                    using (var ms = new MemoryStream(tblBytes))
                    {
                        var bf = new BinaryFormatter();
                        table = (Table)bf.Deserialize(ms);
                    }

                    Console.WriteLine($"[Server] Deserijalizovano {table.TableOrders.Count} stavki.");

                    // 4) Prosledi u SendOrderForPreparationService
                    prepService.SendOrder(
                        waiterId,
                        table.TableNumber,
                        table.TableOrders);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] UDP OrderListener: {ex.Message}");
                }
            }
        }

        //private static void StartClientListener(
        //        IClientDirectory directory,
        //        NotificationService notifier,
        //        int registerPort,
        //        int readyPort,
        //        int billPort)
        //{
        //    // 1) Kreiramo dva ne‑blokirajuća TCP soketa: jedan za REGISTER, jedan za READY
        //    var registerSock = new Socket(
        //        AddressFamily.InterNetwork,
        //        SocketType.Stream,
        //        ProtocolType.Tcp);
        //    registerSock.Bind(new IPEndPoint(IPAddress.Any, registerPort));
        //    registerSock.Listen(100);
        //    registerSock.Blocking = true;

        //    var readySock = new Socket(
        //        AddressFamily.InterNetwork,
        //        SocketType.Stream,
        //        ProtocolType.Tcp);
        //    readySock.Bind(new IPEndPoint(IPAddress.Any, readyPort));
        //    readySock.Listen(100);
        //    readySock.Blocking = false;

        //    var billSock = new Socket(
        //        AddressFamily.InterNetwork,
        //        SocketType.Stream,
        //        ProtocolType.Tcp);
        //    billSock.Bind(new IPEndPoint(IPAddress.Any, billPort));
        //    billSock.Listen(100);
        //    billSock.Blocking = false;

        //    Console.WriteLine($"[Server] Socket za registraciju na portu {registerPort}");
        //    Console.WriteLine($"[Server] Socket za zavrsene porudzbine socket na portu {readyPort}");
        //    Console.WriteLine($"[Server] Socket za racun na portu {billPort}");

        private static void StartClientListener(
      IClientDirectory directory,
      NotificationService notifier,
      int registerPort,
      int readyPort,
      int billPort)
        {
            // 1) Kreiramo dva ne‑blokirajuća TCP soketa: jedan za REGISTER, jedan za READY
            var registerSock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            registerSock.Bind(new IPEndPoint(IPAddress.Any, registerPort));
            registerSock.Listen(100);
            registerSock.Blocking = false;

            var readySock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            readySock.Bind(new IPEndPoint(IPAddress.Any, readyPort));
            readySock.Listen(100);
            readySock.Blocking = false;

            var billSock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            billSock.Bind(new IPEndPoint(IPAddress.Any, billPort));
            billSock.Listen(100);
            billSock.Blocking = false;

            Console.WriteLine($"[Server] REGISTER socket na portu {registerPort}");
            Console.WriteLine($"[Server] READY    socket na portu {readyPort}");



            try
            {


                while (true)
                {
                    List<Socket> readList = new List<Socket> { registerSock, readySock };
                    List<Socket> errorList = new List<Socket>() { registerSock, readySock };



                    // 3) Select čeka do 1 sekunde na događaje za čitanje
                    Socket.Select(readList, null, errorList, 1000 * 1000);

                    if (readList.Count > 0)
                    {
                        foreach (var s in readList)
                        {
                            var buffer = new byte[1024];
                            if (s == registerSock)
                            {
                                // neko pokušava da se registruje
                                Socket client = registerSock.Accept();
                                client.Blocking = true;
                                // primamo REGISTER liniju

                                int r = client.Receive(buffer);
                                var line = Encoding.UTF8.GetString(buffer, 0, r).Trim();
                                // očekujemo "REGISTER;{id};{type};{udpPort}"
                                var parts = line.Split(';');

                                if (parts.Length == 4 && parts[0] == "REGISTER")
                                {
                                    int id = int.Parse(parts[1]);
                                    var type = (ClientType)Enum.Parse(
                                                       typeof(ClientType),
                                                       parts[2],
                                                       true);
                                    int udpPort = int.Parse(parts[3]);
                                    var ip = ((IPEndPoint)client.RemoteEndPoint).Address;

                                    directory.Register(new ClientInfo
                                    {
                                        Id = id,
                                        Type = type,
                                        Socket = client,
                                        Endpoint = new IPEndPoint(ip, udpPort)
                                    });

                                    // vratimo potvrdu
                                    var ok = Encoding.UTF8.GetBytes("REGISTERED\n");
                                    //IPEndPoint remoteEp = new IPEndPoint(ip,udpPort);
                                    //client.Connect(remoteEp);
                                    client.Send(ok);
                                    Console.WriteLine(
                                        $"[Server] Registrovan klijent: ID={id}, Tip={type}, UDPport={udpPort}");
                                }
                                else
                                {
                                    //var bad = Encoding.UTF8.GetBytes("INVALID_REGISTER\n");
                                    //client.Send(bad);
                                    Console.WriteLine($"Neuspjensa registracija [{line}]");
                                }
                                //client.Close();
                            }
                            else if (s == readySock)
                            {
                                // neko šalje READY poruku
                                Socket client = readySock.Accept();
                                client.Blocking = true;
                                int r = client.Receive(buffer);
                                var line = Encoding.UTF8.GetString(buffer, 0, r).Trim();
                                // očekujemo "READY;{tableId};{waiterId}"
                                var parts = line.Split(';');
                                if (parts.Length == 4 && parts[0] == "READY")
                                {
                                    int tableId = int.Parse(parts[1]);
                                    int waiterId = int.Parse(parts[2]);
                                    string tipPorudzbine = parts[3];
                                    Console.WriteLine(
                                        $"[Server]Porudzbina {tipPorudzbine} za konobara #{waiterId} za sto {tableId} je gotova");
                                    notifier.NotifyOrderReady(tableId, waiterId);
                                }
                                else
                                {
                                    Console.WriteLine($"[Server] Neočekivana poruka na READY socket: {line}");
                                }
                                //client.Close();
                            }
                        }
                    }
                    if (errorList.Count > 0)
                    {
                        Console.WriteLine($"Desilo se {errorList.Count} gresaka\n");

                        foreach (Socket s in errorList)
                        {
                            Console.WriteLine($"Greska na socketu: {s.LocalEndPoint}");

                            Console.WriteLine("Zatvaram socket zbog greske...");
                            s.Close();

                        }
                    }

                    // 4) Obrada svakog soketa koji je spreman


                }
            }
            catch (SocketException ex) { Console.WriteLine($"Doslo je do greske: {ex.Message}"); }
        }


        //    var readList = new List<Socket>();

        //    while (true)
        //    {
        //        // 2) Pripremimo listu za Select
        //        readList.Clear();
        //        readList.Add(registerSock);
        //        readList.Add(readySock);
        //        readList.Add(billSock);

        //        // 3) Select čeka do 1 sekunde na događaje za čitanje
        //        Socket.Select(readList, null, null, microSeconds: 1_000_000);

        //        if (readList.Count == 0)
        //        {
        //            // timeout, nema događaja
        //            continue;
        //        }

        //        // 4) Obrada svakog soketa koji je spreman
        //        foreach (Socket s in readList)
        //        {
        //            Console.WriteLine("prolazak kroz petlju");
        //            var buffer = new byte[1024];
        //            if (s == registerSock)
        //            {
        //                Console.WriteLine("Pokusaj registracije");
        //                // neko pokušava da se registruje
        //                Socket client = registerSock.Accept();
        //                client.Blocking = true;
        //                // primamo REGISTER liniju

        //                int r = client.Receive(buffer);
        //                var line = Encoding.UTF8.GetString(buffer, 0, r).Trim();
        //                // očekujemo "REGISTER;{id};{type};{udpPort}"
        //                var parts = line.Split(';');

        //                if (parts.Length == 4 && parts[0] == "REGISTER")
        //                {
        //                    int id = int.Parse(parts[1]);
        //                    var type = (ClientType)Enum.Parse(
        //                                       typeof(ClientType),
        //                                       parts[2],
        //                                       true);
        //                    int udpPort = int.Parse(parts[3]);
        //                    var ip = ((IPEndPoint)client.RemoteEndPoint).Address;

        //                    directory.Register(new ClientInfo
        //                    {
        //                        Id = id,
        //                        Type = type,
        //                        Socket = client,
        //                        Endpoint = new IPEndPoint(ip, udpPort)
        //                    });

        //                    // vratimo potvrdu
        //                    var ok = Encoding.UTF8.GetBytes("REGISTERED\n");
        //                    //IPEndPoint remoteEp = new IPEndPoint(ip,udpPort);
        //                    //client.Connect(remoteEp);
        //                    client.Send(ok);
        //                    Console.WriteLine(
        //                        $"[Server] Registrovan klijent: ID={id}, Tip={type}, UDPport={udpPort}");
        //                }
        //                else
        //                {
        //                    //var bad = Encoding.UTF8.GetBytes("INVALID_REGISTER\n");
        //                    //client.Send(bad);
        //                    Console.WriteLine($"Neuspjensa registracija [{line}]");
        //                }
        //                //client.Close();
        //            }

        //            else if (s == readySock)
        //            {
        //                // neko šalje READY poruku
        //                Socket client = readySock.Accept();
        //                client.Blocking = true;
        //                int r = client.Receive(buffer);
        //                var line = Encoding.UTF8.GetString(buffer, 0, r).Trim();
        //                // očekujemo "READY;{tableId};{waiterId}"
        //                var parts = line.Split(';');
        //                if (parts.Length == 4 && parts[0] == "READY")
        //                {
        //                    int tableId = int.Parse(parts[1]);
        //                    int waiterId = int.Parse(parts[2]);
        //                    string tipPorudzbine = parts[3];
        //                    Console.WriteLine(
        //                        $"[Server]Porudzbina {tipPorudzbine} za konobara #{waiterId} za sto {tableId} je gotova");
        //                    notifier.NotifyOrderReady(tableId, waiterId);
        //                }
        //                else
        //                {
        //                    Console.WriteLine($"[Server] Neočekivana poruka na READY socket: {line}");
        //                }
        //            }
        //                    else if (s == billSock)
        //                    {
        //                        Socket client = billSock.Accept();
        //        client.Blocking = true;
        //                        int r = client.Receive(buffer);
        //        string line = Encoding.UTF8.GetString(buffer, 0, r).Trim(); ;
        //                        string[] parts = line.Split(';');
        //        bool pokusaj = Int32.TryParse(parts[2], out int brojStola);
        //                        if (parts[0] == "RACUN")
        //                        {
        //                            //izracunavanje kusura
        //                            CalculateTheBill kasa = new CalculateTheBill();
        //        TableRepository tdb = new TableRepository();
        //        string poruka = kasa.Calculate(brojStola);


        //        client.Send(Encoding.UTF8.GetBytes(poruka));
        //                        }
        //}
        //            else
        //            {
        //                Console.WriteLine("Nepoznata greska");
        //            }

        //            //client.Close();
        //        }
        //    }
        //}


        private static void StartOrderListener(
                        ISendOrderForPreparation prepService,
                        int tcpPort)
        {
            // 1) Kreiramo i pokrećemo TCP server-socket
            var orderSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            orderSocket.Bind(new IPEndPoint(IPAddress.Any, tcpPort));
            orderSocket.Listen(backlog: 10);  // obavezno pozovite Listen

            Console.WriteLine($"[Server] Čeka ORDER poruke na TCP portu {tcpPort}...");

            while (true)
            {
                // 2) Prihvatamo novu konekciju
                var client = orderSocket.Accept();

                new Thread(() =>
                {
                    try
                    {
                        // 3) Primamo celu poruku (ORDER;waiter;table;base64…\n)
                        var buffer = new byte[8192];
                        int bytesReceived = client.Receive(buffer);
                        if (bytesReceived <= 0) return;

                        // 4) Parsiramo tekst
                        var line = Encoding.UTF8
                            .GetString(buffer, 0, bytesReceived)
                            .Trim();  // uklanja CR/LF i razmake

                        // očekujemo tačno 4 dela
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

                        // 5) Base64 → bajtovi
                        byte[] tableData = Convert.FromBase64String(b64);

                        // 6) Deserijalizacija u objekat Table
                        Domain.Models.Table table;
                        using (var ms = new MemoryStream(tableData))
                        {
                            var bf = new BinaryFormatter();
                            table = (Domain.Models.Table)bf.Deserialize(ms);
                        }

                        Console.WriteLine(
                            $"[Server] Porudzbina od konobara {waiterId} za sto {table.TableNumber}, " +
                            $"{table.TableOrders.Count} stavki.");

                        // 7) Prosleđujemo u servis
                        prepService.SendOrder(
                            waiterId,
                            table.TableNumber,
                            table.TableOrders);
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

        //private static void StartOrderListener(ISendOrderForPreparation prepService, int tcpPort)
        //{
        //    Socket orderSocket = new Socket(
        //                AddressFamily.InterNetwork,
        //                SocketType.Stream,
        //                ProtocolType.Tcp
        //                );

        //    orderSocket.Bind(new IPEndPoint(IPAddress.Any, tcpPort));
        //    while (true)
        //    {
        //        //radnik koji salje poruku
        //        Socket client = orderSocket.Accept();
        //        new Thread(() =>
        //        {
        //            //prihvatamo poruku
        //            byte[] buffer = new byte[1024];
        //            int bytesRecieved = client.Receive(buffer);

        //            //obrada
        //            string line = Encoding.UTF8.GetString(buffer).Trim();




        //            using (var stream = client.GetStream())
        //            {
        //                try
        //                {
        //                    //1) Čitaj waiterId
        //                    int idLen = ReadIntFromStream(stream);
        //                    byte[] idBuf = ReadBytesFromStream(stream, idLen);
        //                    int waiterId = int.Parse(Encoding.UTF8.GetString(idBuf));

        //                    // 2) Čitaj serijalizovani Table
        //                    int tblLen = ReadIntFromStream(stream);
        //                    byte[] tblBuf = ReadBytesFromStream(stream, tblLen);

        //                    // 3) Debug: ispiši kao Base64
        //                    string base64 = Convert.ToBase64String(tblBuf);
        //                    // Console.WriteLine($"[SERVER] Received raw ORDER;{waiterId};<binary {tblLen} bytes>");
        //                    Console.WriteLine($"           BASE64 (start): {base64.Substring(0, Math.Min(80, base64.Length))}…");

        //                    // 4) Deserijalizuj i izvuci Order listu
        //                    Table table;
        //                    using (var ms = new MemoryStream(tblBuf))
        //                    {
        //                        var bf = new BinaryFormatter();
        //                        table = (Table)bf.Deserialize(ms);
        //                    }

        //                    Console.WriteLine(
        //                        $"[SERVER] Porudžbina od konobara {waiterId} za sto {table.TableNumber}, " +
        //                        $"{table.TableOrders.Count} stavki.");

        //                    // 5) Prosledi u servis za pripremu
        //                    prepService.SendOrder(
        //                        waiterId,
        //                        table.TableNumber,
        //                        table.TableOrders);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"[ERROR] OrderListener: {ex.GetType().Name}: {ex.Message}");
        //                }
        //                finally
        //                {
        //                    client.Close();
        //                }
        //            }
        //        })
        //        { IsBackground = true }
        //        .Start();
        //    }
        //}

        /// <summary>
        /// Listener koji prihvata klijente (konobare, kuvare, barmene),
        /// obrađuje REGISTER i READY poruke.
        /// </summary>
        // private static void StartClientListenerr(
        //         IClientDirectory directory,
        //         NotificationService notifier,
        //         int tcpPort
        //         )
        //{
        //    var listener = new TcpListener(IPAddress.Any, tcpPort);
        //    listener.Start();

        //    while (true)
        //    {
        //        var client = listener.AcceptTcpClient();
        //        new Thread(() =>
        //        {
        //            using (var stream = client.GetStream())
        //            using (var reader = new StreamReader(stream, Encoding.UTF8))
        //            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
        //            {
        //                try
        //                {
        //                    // REGISTRACIJA: "REGISTER;{id};{type};{udpPort}"
        //                    var regLine = reader.ReadLine();
        //                    var parts = regLine?.Split(';') ?? new string[0];
        //                    if (parts.Length == 4 && parts[0] == "REGISTER")
        //                    {
        //                        int id = int.Parse(parts[1]);
        //                        var type = (ClientType)Enum.Parse(typeof(ClientType), parts[2], true);
        //                        int udpPort = int.Parse(parts[3]);


        //                        var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address;


        //                        directory.Register(new ClientInfo
        //                        {
        //                            Id = id,
        //                            Type = type,
        //                            Socket = client,
        //                            UdpEndpoint = new IPEndPoint(clientIp, udpPort)
        //                        });

        //                        writer.WriteLine("REGISTERED");
        //                        Console.WriteLine($"[SERVER] Registrovan klijent: ID={id}, Tip={type}, UDPport={udpPort}");
        //                    }
        //                    else
        //                    {
        //                        writer.WriteLine("INVALID_REGISTER");
        //                        return;
        //                    }

        //                    // OSLUŠKUJ READY poruke
        //                    string line;
        //                    while ((line = reader.ReadLine()) != null)
        //                    {
        //                        var msg = line.Split(';');
        //                        if (msg[0] == "READY")
        //                        {
        //                            int tableId = int.Parse(msg[1]);
        //                            int waiterId = int.Parse(msg[2]);
        //                            Console.WriteLine($"[SERVER] Primljena informacija o zavrsenoj porudzbini Sto={tableId}, Konobar={waiterId}");
        //                            notifier.NotifyOrderReady(tableId, waiterId);
        //                            Console.WriteLine(
        //                                $"[SERVER] Obaveštenje poslato konobaru {waiterId} za sto {tableId}");

        //                            //WaiterRepository repo = new WaiterRepository();
        //                            //repo.SetWaiterState(waiterId, true);
        //                        }
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    Console.WriteLine($"[ERROR] ClientListener: {ex.GetType().Name}: {ex.Message}");
        //                }
        //                finally
        //                {
        //                    client.Close();
        //                    // Po potrebi: directory.Unregister(...)
        //                }
        //            }
        //        })
        //        { IsBackground = true }
        //        .Start();
        //    }
        //}

        /// <summary>
        /// Čita 4-bajtni length prefix iz toka.
        /// </summary>
        private static int ReadIntFromStream(Stream stream)
        {
            var buf = new byte[4];
            stream.Read(buf, 0, 4);
            return BitConverter.ToInt32(buf, 0);
        }

        /// <summary>
        /// Čita tačno <paramref name="count"/> bajtova iz toka.
        /// </summary>
        private static byte[] ReadBytesFromStream(Stream stream, int count)
        {
            var buf = new byte[count];
            int offset = 0, remaining = count;
            while (remaining > 0)
            {
                int read = stream.Read(buf, offset, remaining);
                if (read <= 0) throw new EndOfStreamException();
                offset += read;
                remaining -= read;
            }
            return buf;
        }

    }
}
