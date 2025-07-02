using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Cook
{
    class Cook
    {
        static void Main(string[] args)
        {
            // args: [0]=ClientId, [1]=CountOfCooks, [2]=udpPort (ne koristi se ovde)
            int.TryParse(args[0], out int cookId);
            Console.WriteLine($"Cook number #{args[1]}, clientId #{cookId}, Port #{args[2]}");

            const string serverIp = "127.0.0.1";
            const int serverPort = 5000; // isti port na kojem server sluša registraciju

            using (var tcp = new TcpClient(serverIp, serverPort))
            using (var reader = new StreamReader(tcp.GetStream(), Encoding.UTF8))
            using (var writer = new StreamWriter(tcp.GetStream(), Encoding.UTF8) { AutoFlush = true })
            {
                // Registracija kod servera
                writer.WriteLine($"REGISTER;{cookId};Cook;0");
                if (reader.ReadLine() != "REGISTERED")
                    return;

                Console.WriteLine("Cook je registrovan i čeka porudžbine...");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("PREPARE;")) continue;

                    // PREPARE;{sto};{artikal}
                    // npr. "PREPARE;2;1;Karadjordjeva,Karadjordjeva,Burek"
                    var parts = line.Split(new[] { ';' }, 4);
                    int tableNo = int.Parse(parts[1]);
                    int waiter = int.Parse(parts[2]);
                    string items = parts[3];

                    Console.WriteLine($"Porudžbina za sto {tableNo} od konobara {waiter}: {items}");
                    Thread.Sleep(2000); // simulacija pripreme
                    Console.WriteLine($"Cook završio porudžbinu: Sto={tableNo},Konobar={waiter},Artikli={items}");

                    // Notify server da je READY
                    writer.WriteLine($"READY;{tableNo};{waiter}");
                }
            }
        }
    }
}
