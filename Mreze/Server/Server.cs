using System.Threading;
using Domain.Helpers;
using Domain.Services;
using Services.ServerServices;
using Services.TakeATableServices;
using Services.TakeOrdersFromWaiterServices;
using Services.SendOrderForPreparationServices;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Services.NotificationServices;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Infrastructure.Networking;
using System.Linq;
namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            // Inicijalizacija repozitorijuma i servisa
            IClientDirectory clientDirectory = new ClientDirectory();
            NotificationService notificationService = new NotificationService(clientDirectory);

            // Pokretanje TCP listenera za klijente
            Thread listenerThread = new Thread(() => StartClientListener(clientDirectory, notificationService));
            listenerThread.Start();

            // Pokretanje postojećih poslovnih niti
            IReadService readService = new ServerReadTablesService();
            ITakeATableServerService tableService = new TakeATableServerService(readService);
            ISendOrderForPreparation prepService = new SendOrderForPreparationService(3, 3);

            new Thread(() => tableService.TakeATable()).Start();
            new Thread(() => new TakeOrdersFromWaiterService(readService, prepService).ProcessAnOrder()).Start();

            // Kreiranje inicijalnih instanci klijenata
            CreateClientInstance createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, ClientType.Waiter);
            createClientInstance.BrojITipKlijenta(1, ClientType.Cook);
            createClientInstance.BrojITipKlijenta(1, ClientType.Bartender);

            listenerThread.Join();
        }

        private static void StartClientListener(IClientDirectory clientDirectory, NotificationService notifier)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Console.WriteLine("TCP listener pokrenut na portu 5000.");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Novi klijent povezan.");
                new Thread(() => HandleClient(client, clientDirectory, notifier)).Start();
            }
        }

        private static void HandleClient(TcpClient tcpClient, IClientDirectory directory, NotificationService notifier)
        {
            // Koristimo using statements umesto C# 8 using declarations
            using (NetworkStream stream = tcpClient.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                // Registracija: očekujemo FORMAT "REGISTER;{id};{type};{udpPort}"
                string regLine = reader.ReadLine();
                var parts = regLine?.Split(';');
                if (parts == null || parts.Length != 4 || parts[0] != "REGISTER")
                {
                    writer.WriteLine("INVALID_REGISTER");
                    tcpClient.Close();
                    return;
                }

                int id = int.Parse(parts[1]);
                // Korišćenje stare Enum.Parse metode sa cast-om
                ClientType clientType = (ClientType)Enum.Parse(typeof(ClientType), parts[2], true);
                int udpPort = int.Parse(parts[3]);
                IPAddress ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;

                var info = new ClientInfo
                {
                    Id = id,
                    Type = clientType,
                    Socket = tcpClient,
                    UdpEndpoint = new IPEndPoint(ip, udpPort)
                };

                directory.Register(info);
                writer.WriteLine("REGISTERED");
                Console.WriteLine($"Registrovan klijent: ID={id}, Tip={clientType}, UDPPort={udpPort}");

                // Osluškivanje poruka
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var msg = line.Split(';');
                    switch (msg[0])
                    {
                        case "ORDER":
                            HandleOrder(msg, directory);
                            break;
                        case "READY":
                            int tableId = int.Parse(msg[1]);
                            int waiterId = int.Parse(msg[2]);
                            notifier.NotifyOrderReady(tableId, waiterId);
                            Console.WriteLine($"Obaveštenje poslato konobaru {waiterId} za sto {tableId}.");
                            break;
                        default:
                            Console.WriteLine($"Nepoznata poruka: {line}");
                            break;
                    }
                }
            }

            // Nakon prekida konekcije, uklanjamo klijenta iz repozitorijuma
            var allClients = directory.GetByType(ClientType.Waiter)
                .Concat(directory.GetByType(ClientType.Cook))
                .Concat(directory.GetByType(ClientType.Bartender));
            var disconnected = allClients.FirstOrDefault(c => c.Socket == tcpClient);
            if (disconnected != null)
            {
                directory.Unregister(disconnected.Id);
                Console.WriteLine("Klijent se odjavio.");
            }
        }

        private static void HandleOrder(string[] msg, IClientDirectory directory)
        {
            int tableId = int.Parse(msg[1]);
            string item = msg[2];
            string category = msg[3];

            ClientType targetType = category.Equals("Drink", StringComparison.OrdinalIgnoreCase)
                ? ClientType.Bartender : ClientType.Cook;

            var target = directory.GetByType(targetType).FirstOrDefault();
            if (target != null)
            {
                string payload = $"PREPARE;{tableId};{item}\n";
                byte[] data = Encoding.UTF8.GetBytes(payload);
                target.Socket.GetStream().Write(data, 0, data.Length);
                Console.WriteLine($"Poslato {item} {targetType}u (ID={target.Id}) za sto {tableId}");
            }
            else
            {
                Console.WriteLine($"Nema slobodnog {targetType}a za sto {tableId}.");
            }
        }
        /*
        void FakeMain()
        {
            //Postavljanje repozitorijuma i notifikacionog servisa
            


            //inject WaiterRepository svugde gde treba, da bi Server mogao da proverava u kojem su stanju kad mu zatrebaju

            IReadService iReadService = new ServerReadTablesService();

            ITakeATableServerService iTakeTableServerService = new TakeATableServerService(iReadService);
            ISendOrderForPreparation sendOrderForPreparationService = new SendOrderForPreparationService(3,3);

            Thread serverTakeTableThread = new Thread(() => iTakeTableServerService.TakeATable());
            serverTakeTableThread.Start();

            ITakeOrdersFromWaiterService iTakeOrdersFromWaiterService = new TakeOrdersFromWaiterService(iReadService, sendOrderForPreparationService);
            Thread serverTakeOrdersFromWaiter = new Thread(() => iTakeOrdersFromWaiterService.ProcessAnOrder());
            serverTakeOrdersFromWaiter.Start();

            CreateClientInstance createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, "konobar");
            createClientInstance.BrojITipKlijenta(1, "kuvar");
            createClientInstance.BrojITipKlijenta(1, "barmen");

            ////////////////////////////////za sada se threadovi ne terminiraju na kraju(server se ne gasi sam), nisam siguran jos kako cemo simulirati kraj, zavisi od toga kako ce ceo sistem raditi tj koliko ce biti konobara i sta sve oni mogu da rade, mozda ce biti da se server tj program gasi kada se svi konobari odjave
            //otici u WaiterManagementService


            //Console.WriteLine("end");
            //Console.ReadKeyx
        }
        /
        */

    }
}
