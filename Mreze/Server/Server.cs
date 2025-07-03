using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Repositories.OrderRepository;
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

            // 2) Pokretanje i registrovanje klijentskih procesa
            var createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, ClientType.Waiter);
            createClientInstance.BrojITipKlijenta(1, ClientType.Cook);
            createClientInstance.BrojITipKlijenta(1, ClientType.Bartender);
            createClientInstance.BrojITipKlijenta(1, ClientType.Manager);

            // 3.1) UDP listener za raspodelu stolova
            var readService = new ServerReadTablesService();
            var tableService = new TakeATableServerService(readService, listenPort: 4000);
            new Thread(tableService.TakeATable) { IsBackground = true }.Start();
            Console.WriteLine("[Server] UDP TableListener pokrenut na portu 4000.");

            // 3.11) UDP listener za otkazivanje rezervacija koje su istekle na portu 4001 (menadzer salje serveru da je rezervacija otkazana - istekla, ako u roku od 10 sekundi ne dodju gosti)

            var releaseATableService = new ReleaseATableServerService(readService, 4001);
            releaseATableService.ReleaseATable();   //Listener thread se nalazi u servisu (ne pravi se u Serveru). Nema neke razlike ali ovako je mozda cistije
            //I da napravimo jos 2 servisa za ove dole TCP sto su nam static
            Console.WriteLine("[Server] UDP TableCancelationListener pokrenut na portu 4001.");

            // 3.12) bice UDP listener za goste koji su stigli sa rezervacijom na portu 4002 (menadzer salje serveru da su stigli)



            // 3.2) TCP listener za porudžbine na portu 15000
            new Thread(() => StartOrderListener(prepService, tcpPort: 15000)) { IsBackground = true }
                .Start();
            Console.WriteLine("[Server] TCP OrderListener pokrenut na portu 15000.");

            // 3.3) TCP listener za registraciju i notifikacije na portu 5000
            new Thread(() => StartClientListener(
                clientDirectory,
                notificationService,
                tcpPort: 5000))
            {
                IsBackground = true
            }.Start();
            Console.WriteLine("[Server] TCP ClientListener pokrenut na portu 5000.");

            // 4) Držimo main nit živom
            Console.WriteLine("Server je pokrenut. Pritisni ENTER za zaustavljanje.");
            Console.ReadLine();
        }

        /// <summary>
        /// Listener koji prima binarni protokol: length-prefixed waiterId i serijalizovani Table.
        /// Deserijalizuje ih, loguje kao Base64 i potom prosleđuje prepService.SendOrder.
        /// gornji thread pravi novi thread, da on moze da obavlja posao bez da blokira ostale
        /// </summary>
        private static void StartOrderListener(ISendOrderForPreparation prepService, int tcpPort)
        {
            var listener = new TcpListener(IPAddress.Any, tcpPort);
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                new Thread(() =>
                {
                    using (var stream = client.GetStream())
                    {
                        try
                        {
                            //1) Čitaj waiterId
                            int idLen = ReadIntFromStream(stream);
                            byte[] idBuf = ReadBytesFromStream(stream, idLen);
                            int waiterId = int.Parse(Encoding.UTF8.GetString(idBuf));

                            // 2) Čitaj serijalizovani Table
                            int tblLen = ReadIntFromStream(stream);
                            byte[] tblBuf = ReadBytesFromStream(stream, tblLen);

                            // 3) Debug: ispiši kao Base64
                            string base64 = Convert.ToBase64String(tblBuf);
                            // Console.WriteLine($"[SERVER] Received raw ORDER;{waiterId};<binary {tblLen} bytes>");
                            Console.WriteLine($"           BASE64 (start): {base64.Substring(0, Math.Min(80, base64.Length))}…");

                            // 4) Deserijalizuj i izvuci Order listu
                            Table table;
                            using (var ms = new MemoryStream(tblBuf))
                            {
                                var bf = new BinaryFormatter();
                                table = (Table)bf.Deserialize(ms);
                            }

                            Console.WriteLine(
                                $"[SERVER] Porudžbina od konobara {waiterId} za sto {table.TableNumber}, " +
                                $"{table.TableOrders.Count} stavki.");

                            // 5) Prosledi u servis za pripremu
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
                    }
                })
                { IsBackground = true }
                .Start();
            }
        }

        /// <summary>
        /// Listener koji prihvata klijente (konobare, kuvare, barmene),
        /// obrađuje REGISTER i READY poruke.
        /// </summary>
        private static void StartClientListener(
            IClientDirectory directory,
            NotificationService notifier,
            int tcpPort)
        {
            var listener = new TcpListener(IPAddress.Any, tcpPort);
            listener.Start();

            while (true)
            {
                var client = listener.AcceptTcpClient();
                new Thread(() =>
                {
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        try
                        {
                            // REGISTRACIJA: "REGISTER;{id};{type};{udpPort}"
                            var regLine = reader.ReadLine();
                            var parts = regLine?.Split(';') ?? new string[0];
                            if (parts.Length == 4 && parts[0] == "REGISTER")
                            {
                                int id = int.Parse(parts[1]);
                                var type = (ClientType)Enum.Parse(typeof(ClientType), parts[2], true);

                                directory.Register(new ClientInfo
                                {
                                    Id = id,
                                    Type = type,
                                    Socket = client
                                });
                                writer.WriteLine("REGISTERED");
                                Console.WriteLine($"[SERVER] Registrovan klijent: ID={id}, Tip={type}");
                            }
                            else
                            {
                                writer.WriteLine("INVALID_REGISTER");
                                return;
                            }

                            // OSLUŠKUJ READY poruke
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var msg = line.Split(';');
                                if (msg[0] == "READY")
                                {
                                    int tableId = int.Parse(msg[1]);
                                    int waiterId = int.Parse(msg[2]);
                                    Console.WriteLine($"[SERVER] Primljena informacija o zavrsenoj porudzbini Sto={tableId}, Konobar={waiterId}");
                                    notifier.NotifyOrderReady(tableId, waiterId);
                                    Console.WriteLine(
                                        $"[SERVER] Obaveštenje poslato konobaru {waiterId} za sto {tableId}");

                                    //WaiterRepository repo = new WaiterRepository();
                                    //repo.SetWaiterState(waiterId, true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] ClientListener: {ex.GetType().Name}: {ex.Message}");
                        }
                        finally
                        {
                            client.Close();
                            // Po potrebi: directory.Unregister(...)
                        }
                    }
                })
                { IsBackground = true }
                .Start();
            }
        }

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
