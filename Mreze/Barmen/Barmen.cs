using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using Domain.Models;

namespace Barmen
{
    class Barmen
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("[Pokretanje Barmen.exe nije uspelo]");
                return;
            }

            int barmenId = int.Parse(args[0]);
            int count = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"[Sanker] id#{barmenId}, Port {udpPort} ");

            const string serverIp = "127.0.0.1";
            const int registerPort = 5000;    // port na kojem se REGISTER prima
            const int readyPort = 5001;    // port na kojem se PREPARE i READY razmenjuju

            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), registerPort));

            string regMsg = $"REGISTER;{barmenId};Bartender;{udpPort}\n";
            sock.Send(Encoding.UTF8.GetBytes(regMsg));

            var ackBuf = new byte[8192];
            int bytesRecvd = sock.Receive(ackBuf);
            string ack = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

            if (ack != "REGISTERED")
            {
                Console.WriteLine($"\n[Sanker] REGISTRACIJA NEUSPESNA: {ack}");
                sock.Close();
                return;
            }

            Console.WriteLine("\n[Sanker] USPESNO REGISTROVAN, CEKAM NARUDZBINE...");

            Thread.Sleep(5000);
            var orderSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Thread.Sleep(10000);
            orderSock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), readyPort));

            while (true)
            {
                bytesRecvd = sock.Receive(ackBuf);
                if (bytesRecvd <= 0)
                {
                    Console.WriteLine("[Sanker] Veza je prekinuta od strane servera.");
                    break;
                }

                string msg = Encoding.UTF8.GetString(ackBuf, 0, bytesRecvd).Trim();

                if (!msg.StartsWith("PREPARE;"))
                    continue;

                var parts = msg.Split(new[] { ';' }, 4);
                int tableNo = int.Parse(parts[1]);
                int waiter = int.Parse(parts[2]);
                string b64 = parts[3];

                byte[] orderData = Convert.FromBase64String(b64);
                List<Order> ordered;
                using (var ms = new MemoryStream(orderData))
                {
                    var bf = new BinaryFormatter();
                    ordered = (List<Order>)bf.Deserialize(ms);
                }

                Console.WriteLine(
                    $"[Sanker] Porudzbina za sto {tableNo} od konobara {waiter}:");
                foreach (Order o in ordered)
                {
                    Console.WriteLine($"{o.ToString()}");
                }

                foreach (var o in ordered)
                {
                    o._articleStatus = ArticleStatus.PRIPREMA;
                    Console.WriteLine($"[Sanker] Priprema u toku : {o._articleName}");
                    Thread.Sleep(new Random().Next(500, 2000));
                    o._articleStatus = ArticleStatus.SPREMNO;
                    Console.WriteLine($"[Sanker] Priprema gotova: {o._articleName}");
                }

                Console.WriteLine($"[Sanker] NARUDŽBINA KOMPLETIRANA STO:{tableNo} KONOBAR:{waiter}");

                string readyMsg = $"READY;{tableNo};{waiter};pice\n";
                orderSock.Send(Encoding.UTF8.GetBytes(readyMsg));
                Console.WriteLine("[Sanker] Server obavješten o zavrsetku porudzbine");
            }

            sock.Close();
            orderSock.Close();
        }
    }
}
