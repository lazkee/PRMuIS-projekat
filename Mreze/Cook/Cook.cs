using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Cook
{
    class Cook
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Upotreba: Cook <ClientId> <Count> <UdpPort>");
                return;
            }

            int cookId = int.Parse(args[0]);
            int count = int.Parse(args[1]);
            int udpPort = int.Parse(args[2]);

            Console.WriteLine($"Cook number #{count}, clientId #{cookId}, sluša UDP port #{udpPort}");

            const string serverIp = "127.0.0.1";
            const int serverPort = 5000;  // TCP port za REGISTER i READY

            // Otvaramo TCP vezu za REGISTER i kasnije slanje READY
            var tcp = new TcpClient(serverIp, serverPort);
            var stream = tcp.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // 1) Registracija kod servera, šaljemo i svoj UDP port
            writer.WriteLine($"REGISTER;{cookId};Cook;{udpPort}");
            var resp = reader.ReadLine();
            if (resp != "REGISTERED")
            {
                Console.WriteLine("Registracija nije uspela, izlazim.");
                return;
            }
            Console.WriteLine("Cook je uspešno registrovan na server.");

            // 2) Pokrećemo UDP listener za PREPARE poruke
            var udpClient = new UdpClient(udpPort);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine($"Čekam PREPARE poruke na UDP portu {udpPort}...");
            while (true)
            {
                try
                {
                    // Primamo celu datagramu
                    var data = udpClient.Receive(ref remoteEP);
                    var msg = Encoding.UTF8.GetString(data).Trim();

                    if (!msg.StartsWith("PREPARE;"))
                        continue;

                    // "PREPARE;{tableNo};{waiterId};{items}"
                    var parts = msg.Split(new[] { ';' }, 4);
                    int tableNo = int.Parse(parts[1]);
                    int waiter = int.Parse(parts[2]);
                    string items = parts[3];

                    Console.WriteLine($"[UDP] Porudžbina za sto {tableNo} od konobara {waiter}: {items}");

                    // Simulacija pripreme
                    Thread.Sleep(2000);
                    Console.WriteLine($"[Cook] Završio pripremu za sto {tableNo}");

                    // 3) Javljamo serveru preko TCP da smo spremni
                    writer.WriteLine($"READY;{tableNo};{waiter}");
                    Console.WriteLine($"[TCP] Poslato READY;{tableNo};{waiter}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] UDP listener: {ex.Message}");
                }
            }

            // Napomena: ne zatvaramo udpClient/tcp ovde jer je petlja beskonačna
        }
    }
}
