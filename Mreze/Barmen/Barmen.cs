using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Barmen
{
    class Barmen
    {
        static void Main(string[] args)
        {
            int bartenderId = int.Parse(args[0]);
            int udpPort = int.Parse(args[1]);
            const string serverAddress = "127.0.0.1";
            const int tcpPort = 5000;

            Console.WriteLine($"Barmen number #{args[1]},");
            Console.WriteLine($"clientId #{args[0]}");
            Console.WriteLine($"Port #{args[2]},");


            try
            {
                // Otvaranje TCP konekcije za registraciju i pripremu poruka
                using (var client = new TcpClient(serverAddress, tcpPort))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Registracija na server
                    writer.WriteLine($"REGISTER;{bartenderId};Bartender;{udpPort}");
                    var response = reader.ReadLine();
                    if (response != "REGISTERED")
                    {
                        Console.WriteLine("Registracija neuspešna.");
                        return;
                    }

                    Console.WriteLine("Čekam narudžbine...");
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("PREPARE;"))
                            continue;

                        // Format: PREPARE;{tableNumber};{itemName}
                        var parts = line.Split(new[] { ';' }, 4);
                        int tableNo = int.Parse(parts[1]);
                        int waiter = int.Parse(parts[2]);
                        string items = parts[3];

                        Console.WriteLine($"NOVA PORUDŽBINA: sto #{tableNo}, artikli: {items}");
                        // Simulacija pripreme
                        Thread.Sleep(1500);
                        Console.WriteLine($"ZAVRŠENO: sto #{tableNo},Konobar #{waiter} ,artikli: {items}");

                        // Notifikacija serveru da je narudžbina spremna
                        writer.WriteLine($"READY;{tableNo};{waiter}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u Barmen: {ex.Message}");
            }
        }
    }
}