using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Helpers;
using Domain.Models;
using Domain.Repositories;
using Domain.Repositories.OrderRepository;
using Domain.Repositories.TableRepository;
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
                readyPort: 5001
                ))
            {
                IsBackground = true
            }.Start();
            Console.WriteLine("[Server] TCP ClientListener pokrenut na portu 5000.");

            

            // 3.1) UDP listener za raspodelu stolova
            var readService = new ServerReadTablesService();
            var tableService = new TakeATableServerService(readService, listenPort: 4000);
            new Thread(tableService.TakeATable) { IsBackground = true }.Start();
            Console.WriteLine("[Server] UDP TableListener pokrenut na portu 4000.");

            CalculateTheBill kasa = new CalculateTheBill();
            new Thread(() => StartBillListener(kasa)) { IsBackground=true}.Start();
            // 3.11) UDP listener za otkazivanje rezervacija koje su istekle na portu 4001 (menadzer salje serveru da je rezervacija otkazana - istekla, ako u roku od 10 sekundi ne dodju gosti)

            var releaseATableService = new ReleaseATableServerService(readService, 4001);
            releaseATableService.ReleaseATable();   //Listener thread se nalazi u servisu (ne pravi se u Serveru). Nema neke razlike ali ovako je mozda cistije
            //I da napravimo jos 2 servisa za ove dole TCP sto su nam static
            Console.WriteLine("[Server] UDP TableCancelationListener pokrenut na portu 4001.");

            // 3.12) bice UDP listener za goste koji su stigli sa rezervacijom na portu 4002 (menadzer salje serveru da su stigli)



            // 3.2) TCP listener za porudžbine na portu 15000
            //new Thread(() => StartOrderListener(prepService, tcpPort: 15000)) { IsBackground = true }
            //    .Start();
            //Console.WriteLine("[Server] TCP OrderListener pokrenut na portu 15000.");
            new Thread(() => StartOrderListener(prepService, 15000)) { IsBackground = true }.Start();
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

        private static void StartBillListener(CalculateTheBill kasa)
        {
            var listenSock = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Stream,
                                        ProtocolType.Tcp);
            listenSock.Bind(new IPEndPoint(IPAddress.Any, 5003));
            listenSock.Listen(100);
            Console.WriteLine($"[Server] Čeka BILL zahteve na portu 5003");

            while (true)
            {
                // 1) Prihvatamo novu konekciju od klijenta
                var client = listenSock.Accept();
                new Thread(() =>
                {
                    try
                    {
                        var buf = new byte[8192];
                        int n;
                        while ((n = client.Receive(buf)) > 0)
                        {


                            if (n <= 0) return;

                            var line = Encoding.UTF8.GetString(buf, 0, n).Trim();
                            string[] parts = line.Split(';');

                            if (parts[0] == "KUSUR")
                            {
                                int br = Int32.Parse(parts[1]);
                                int iznos = Int32.Parse(parts[2]);
                                int uplaceno = Int32.Parse(parts[3]);
                                string poruka = $"[Naplata sto #{br}] Ukupno za uplatu(napojnica uracunata){iznos} Uplaceno:{uplaceno} Vracen kusur:{uplaceno - iznos}";
                                Console.WriteLine($"[Server]{poruka}");
                                client.Send(Encoding.UTF8.GetBytes(poruka));

                                TableRepository.ClearTable(br);
                                Console.WriteLine($"[Server] Sto broj {br} je sada slobodan");

                            }
                            else if (parts[0] == "RACUN")
                            {

                                int brStola = Int32.Parse(parts[1]);
                                Console.WriteLine($"[Server] Sto broj {brStola} je zatrazio racun");
                                string odgovor = kasa.Calculate(brStola);

                                client.Send(Encoding.UTF8.GetBytes(odgovor));
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] BillListener: {ex.Message}{ex.StackTrace}");
                    }
                    finally { client.Close(); }
                    
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

            Console.WriteLine($"[Server] REGISTER socket na portu {registerPort}");
            Console.WriteLine($"[Server] READY    socket na portu {readyPort}");

            try
            {
                while (true)
                {
                    var readList = new List<Socket> { registerSock, readySock };
                    var errorList = new List<Socket> { registerSock, readySock };

                    // 3) Select čeka do 1 sekunde na događaje za čitanje ili greške
                    Socket.Select(readList, null, errorList, 1000);

                    // 4) Obrada svakog soketa koji je spreman
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
                                            notifier.NotifyOrderReady(tableId, waiterId);
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

                    // 5) Obrada grešaka na soketima
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
                        int bytesReceived;
                        while ((bytesReceived = client.Receive(buffer)) > 0)
                        {
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


                            TableRepository.UpdateTable(table);
                            Console.WriteLine(
                                $"[Server] Porudzbina od konobara {waiterId} za sto {table.TableNumber}, " +
                                $"{table.TableOrders.Count} stavki.");

                            // 7) Prosleđujemo u servis
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

       
        //private static int ReadIntFromStream(Stream stream)
        //{
        //    var buf = new byte[4];
        //    stream.Read(buf, 0, 4);
        //    return BitConverter.ToInt32(buf, 0);
        //}

        ///// <summary>
        ///// Čita tačno <paramref name="count"/> bajtova iz toka.
        ///// </summary>
        //private static byte[] ReadBytesFromStream(Stream stream, int count)
        //{
        //    var buf = new byte[count];
        //    int offset = 0, remaining = count;
        //    while (remaining > 0)
        //    {
        //        int read = stream.Read(buf, offset, remaining);
        //        if (read <= 0) throw new EndOfStreamException();
        //        offset += read;
        //        remaining -= read;
        //    }
        //    return buf;
        //}

    }
}
