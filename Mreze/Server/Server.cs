using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Infrastructure.Networking;
using Services;
using Services.NotificationServices;
using Services.SendOrderForPreparationServices;
using Services.ServerServices;
using Services.TakeATableServices;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            // Inicijalizacija repozitorijuma i servisa
            IClientDirectory clientDirectory = new ClientDirectory();
            NotificationService notificationService = new NotificationService(clientDirectory);
            var prepService = new SendOrderForPreparationService(
                numOfChefs: 2,
                numOfBarmens: 1,
                notificationService: notificationService);

            // Pokretanje i registrovanje klijentskih procesa
            var createClientInstance = new CreateClientInstance();
            createClientInstance.BrojITipKlijenta(2, ClientType.Waiter);
            createClientInstance.BrojITipKlijenta(1, ClientType.Cook);
            createClientInstance.BrojITipKlijenta(1, ClientType.Bartender);

            // Pokretanje posebnih listenera:

            // 1) UDP listener za upravljanje stolovima (Take a Table)
            var readService = new ServerReadTablesService();
            var tableService = new TakeATableServerService(readService);
            new Thread(() => tableService.TakeATable()) { IsBackground = true }.Start();

            // 2) TCP listener za porudžbine na portu 15000
            Thread orderThread = new Thread(() => StartOrderListener(prepService, 15000))
            { IsBackground = true };
            orderThread.Start();

            // 3) TCP listener za registraciju i notifikacije na portu 5000
            Thread clientThread = new Thread(() => StartClientListener(
                clientDirectory, prepService, notificationService, 5000));
            clientThread.Start();
            clientThread.Join();
        }

        private static void StartOrderListener(
            SendOrderForPreparationService prepService,
            int port)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Order listener pokrenut na portu {port}.");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread orderHandler = new Thread(() =>
                {
                    try
                    {
                        using (Stream stream = client.GetStream())
                        {
                            int id = ReadIntFromStream(stream);
                            byte[] data = ReadBytesFromStream(stream);
                            Table table;
                            using (var ms = new MemoryStream(data))
                            {
                                table = (Table)new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()
                                    .Deserialize(ms);
                            }

                            prepService.SendOrder(id, table.TableOrders);
                            Console.WriteLine($"Porudžbina od konobara {id} za sto {table.TableNumber} primljena.");
                            Console.WriteLine("| Article name   | Article category | Article price |   status   |"); 
                            foreach (Order order in table.TableOrders)
                            {
                                Console.WriteLine(order);
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška u OrderListener: {ex.Message}");
                    }
                    finally
                    {
                        client.Close();
                    }
                })
                { IsBackground = true };
                orderHandler.Start();
            }
        }

        private static int ReadIntFromStream(Stream stream)
        {
            byte[] lenBuf = new byte[4];
            stream.Read(lenBuf, 0, 4);
            int len = BitConverter.ToInt32(lenBuf, 0);
            byte[] buf = new byte[len];
            stream.Read(buf, 0, len);
            return int.Parse(Encoding.UTF8.GetString(buf));
        }

        private static byte[] ReadBytesFromStream(Stream stream)
        {
            byte[] lenBuf = new byte[4];
            stream.Read(lenBuf, 0, 4);
            int len = BitConverter.ToInt32(lenBuf, 0);
            byte[] buf = new byte[len];
            int read = 0;
            while (read < len)
            {
                read += stream.Read(buf, read, len - read);
            }
            return buf;
        }

        private static void StartClientListener(
            IClientDirectory clientDirectory,
            SendOrderForPreparationService prepService,
            NotificationService notifier,
            int tcpPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, tcpPort);
            listener.Start();
            Console.WriteLine($"Client listener pokrenut na portu {tcpPort}.");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientHandler = new Thread(() => HandleClient(
                    client, clientDirectory, prepService, notifier))
                { IsBackground = true };
                clientHandler.Start();
            }
        }

        private static void HandleClient(
            TcpClient tcpClient,
            IClientDirectory directory,
            SendOrderForPreparationService prepService,
            NotificationService notifier)
        {
            try
            {
                using (Stream stream = tcpClient.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    string regLine = reader.ReadLine();
                    var parts = regLine?.Split(';');
                    if (parts == null || parts.Length != 4 || parts[0] != "REGISTER")
                    {
                        writer.WriteLine("INVALID_REGISTER");
                        return;
                    }

                    int id = int.Parse(parts[1]);
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

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var msg = line.Split(';');
                        if (msg[0] == "READY")
                        {
                            int table = int.Parse(msg[1]);
                            int waiter = int.Parse(msg[2]);
                            notifier.NotifyOrderReady(table, waiter);
                            Console.WriteLine($"Porudžbina za sto {table} gotova, notifikacija konobaru {waiter}.");
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Prekinuta veza sa klijentom: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u HandleClient: {ex}");
            }
            finally
            {
                var all = directory.GetByType(ClientType.Waiter)
                    .Concat(directory.GetByType(ClientType.Cook))
                    .Concat(directory.GetByType(ClientType.Bartender));
                var disconnected = all.FirstOrDefault(c => c.Socket == tcpClient);
                if (disconnected != null)
                {
                    directory.Unregister(disconnected.Id);
                    Console.WriteLine($"Klijent {disconnected.Id} odjavljen.");
                }
                tcpClient.Close();
            }
        }
    }
}
