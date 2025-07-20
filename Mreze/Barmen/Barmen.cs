using Domain.Models;
using Domain.Repositories.TableRepository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace Barmen
{
    class Barmen
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("[Pokretanje Barmen.exe nije uspjelo]");
                return;
            }

            int barmenId = int.Parse(args[0]);
            int count    = int.Parse(args[1]);
            int udpPort  = int.Parse(args[2]);  // server može ignorisati ovaj port

            Console.WriteLine($"[Barmen]  WorkerId #{barmenId}");

            const string serverIp   = "127.0.0.1";
            const int    registerPort = 5000;    // port na kojem se REGISTER prima
            const int    readyPort    = 5001;    // port na kojem se PREPARE i READY razmjenjuju

            // 1) Otvaramo TCP socket za registraciju
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), registerPort));

            // 2) Pošaljemo REGISTER;{barmenId};Bartender;{udpPort}\n
            string regMsg = $"REGISTER;{barmenId};Bartender;{udpPort}\n";
            sock.Send(Encoding.UTF8.GetBytes(regMsg));

            // 3) Prihvatimo odgovor REGISTERED\n
            var ackBuf     = new byte[8192];
            int bytesRecvd = sock.Receive(ackBuf);
            string ack     = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

            if (ack != "REGISTERED")
            {
                Console.WriteLine($"\n[Barmen] REGISTRACIJA NEUSPJESNA: {ack}");
                sock.Close();
                return;
            }

            Console.WriteLine("\n[Barmen] USPJESNO REGISTROVAN, CEKAM NARUDZBINE...");

            // 4) Otvaramo drugi TCP socket za razmjenu PREPARE⇄READY
            var orderSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            orderSock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), readyPort));

            // 5) Beskonačna petlja za PREPARE;… poruke
            while (true)
            {
                bytesRecvd = sock.Receive(ackBuf);
                if (bytesRecvd <= 0)
                {
                    Console.WriteLine("[Barmen] Veza je prekinuta od strane servera.");
                    break;
                }

                // dekodiramo liniju (skidamo whitespace i \r\n)
                string msg = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

                if (!msg.StartsWith("PREPARE;"))
                    continue;

                // PREPARE;{tableNo};{waiterId};{base64OrderData}
                var parts    = msg.Split(new[] { ';' }, 4);
                int    tableNo = int.Parse(parts[1]);
                int    waiter  = int.Parse(parts[2]);
                string b64     = parts[3];

                // 6) Decode + deserialize
                byte[] orderData = Convert.FromBase64String(b64);
                List<Order> ordered;
                using (var ms = new MemoryStream(orderData))
                {
                    var bf     = new BinaryFormatter();
                    ordered = (List<Order>)bf.Deserialize(ms);
                }

                Console.WriteLine(
                    $"[Barmen] Porudzbina za sto {tableNo} od konobara {waiter}:");
                foreach (Order o in ordered)
                {
                    Console.WriteLine($"{o.ToString()}");
                }

                // 7) Simulacija pripreme pića
                foreach (var o in ordered)
                {
                    o._articleStatus = ArticleStatus.INPROGRESS;
                    Console.WriteLine($"[Barmen] Priprema u toku : {o._articleName}");
                    Thread.Sleep(new Random().Next(500, 2000));
                    o._articleStatus = ArticleStatus.FINISHED;
                    Console.WriteLine($"[Barmen] Priprema gotova: {o._articleName}");
                }

                Console.WriteLine($"[Barmen] NARUDŽBINA KOMPLETIRANA STO:{tableNo} KONOBAR:{waiter}");

                // 8) Javljamo serveru preko orderSock da je spremno
                string readyMsg = $"READY;{tableNo};{waiter};pice\n";
                orderSock.Send(Encoding.UTF8.GetBytes(readyMsg));
                Console.WriteLine("[Barmen] Server obavješten o zavrsetku porudzbine");
            }

            sock.Close();
            orderSock.Close();
        }
    }
}
