using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Models;
using Domain.Repositories.TableRepository;

namespace Cook
{
    class Cook
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("[Pokretanje Cook.exe nije uspelo]");
                return;
            }

            int cookId = int.Parse(args[0]);
            int count = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"[Kuvar]  id= #{cookId}, Port {udpPort}");

            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;    // isti port za REGISTER i za PREPARE

            var sock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            int attempts = 0;
            while (true)
            {
                try
                {
                    Console.WriteLine("Pokusavam se konektovati sa serverom");
                    sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), serverPort));
                    break;
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"Greska pri konekciji {e.Message}");
                    if (++attempts >= 5) throw;
                    Thread.Sleep(200);
                }
            }
            ;
            string regMsg = $"REGISTER;{cookId};Cook;{udpPort}\n";
            ;
            try { sock.Send(Encoding.UTF8.GetBytes(regMsg)); } catch (SocketException ex) { Console.WriteLine($"{ex.Message}"); }

            var ackBuf = new byte[8192];
            int bytesRecvd = 0;
            try
            {
                bytesRecvd = sock.Receive(ackBuf);
               
            }
            catch (SocketException ex) { Console.WriteLine($"{ex.Message}"); }
            string ack = Encoding.UTF8
                                    .GetString(ackBuf, 0, bytesRecvd)
                                    .Trim();

            if (ack != "REGISTERED")
            {
                Console.WriteLine($"\n[Kuvar] REGISTRACIJA NEUSPJESNA: {ack}");
                sock.Close();
                return;
            }

            Console.WriteLine("\n[Kuvar] USPESNO REGISTROVAN, CEKAM PORUDZBINE...");

            Thread.Sleep(5000);
            Socket orderSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Thread.Sleep(10000);
            orderSock.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001));
            while (true)
            {
                bytesRecvd = sock.Receive(ackBuf);
                if (bytesRecvd <= 0)
                {
                    Console.WriteLine("[Kuvar] Veza je prekinuta od strane servera.");
                    break;
                }

                string msg = Encoding.UTF8
                              .GetString(ackBuf, 0, bytesRecvd)
                              .Trim();

                if (!msg.StartsWith("PREPARE;"))
                    continue;

                var parts = msg.Split(new[] { ';' }, 4);
                int tableNo = int.Parse(parts[1]);
                int waiter = int.Parse(parts[2]);
                string b64 = parts[3];
                byte[] orderData = Convert.FromBase64String(b64);

                Table table = TableRepository.GetByID(tableNo);

                List<Order> ordered;
                using (var ms = new MemoryStream(orderData))
                {
                    var bf = new BinaryFormatter();
                    ordered = (List<Order>)bf.Deserialize(ms);
                }
                Console.WriteLine(
                    $"[Kuvar] Porudzbina za sto {tableNo} od konobara {waiter}: /n Naruceno je:{ordered.Count} stavki");
                foreach (Order o in ordered)
                {
                    Console.WriteLine($"{o.ToString()}");
                }

                foreach (Order o in ordered)
                {
                    o._articleStatus = ArticleStatus.PRIPREMA;
                    Console.WriteLine($"[Cook] Priprema u toku: {o._articleName}");
                    Random rnd = new Random();
                    Thread.Sleep(rnd.Next(1000, 3000));
                    o._articleStatus = ArticleStatus.SPREMNO;
                    Console.WriteLine($"[Cook] Priprema gotova: {o._articleName}");
                }

                Thread.Sleep(2000);
                Console.WriteLine($"[Kuvar] PORUDZBINA KOMPLETIRANA STO:{tableNo}  KONOBAR{waiter}.");

                string readyMsg = $"READY;{tableNo};{waiter};hrane\n";
                Console.WriteLine("[Kuvar] Server obavesten o zavrsetku porudzbine");
                orderSock.Send(Encoding.UTF8.GetBytes(readyMsg));
            }

            sock.Close();
        }
    }
}
