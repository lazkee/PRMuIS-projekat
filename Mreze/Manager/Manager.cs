using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Domain.Repositories.ManagerRepository;
using Services.ManagementServices;
using Services.ReleaseATableServices;

namespace Manager
{
    public class Manager
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"[FATAL] {((Exception)e.ExceptionObject).Message}");
            };

            Console.WriteLine($"Manager number #{args[1]},");
            Console.WriteLine($"clientId #{args[0]}");
            Console.WriteLine($"Port #{args[2]}");

            int.TryParse(args[0], out int managerId);
            int.TryParse(args[1], out int managerNumber);
            int.TryParse(args[2], out int udpPort);

            // a) Kreiramo TCP socket i povežemo se
            var sock = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(IPAddress.Loopback, 5000));

            // b) Pošaljemo REGISTER;{waiterId};Waiter;{udpPort}\n
            string regMsg = $"REGISTER;{managerId};Manager;{udpPort}\n";
            sock.Send(Encoding.UTF8.GetBytes(regMsg));

            // c) Prihvatimo ACK liniju “REGISTERED\n”
            var tmp = new byte[1];


            byte[] ackbytes = new byte[1024];
            int bytesRecieved = sock.Receive(ackbytes);
            string ack = Encoding.UTF8.GetString(ackbytes, 0, bytesRecieved).Trim();
            if (ack != "REGISTERED")
            {
                Console.WriteLine($"\nREGISTRACIJA NEUSPJESNA");
            }
            else
            {
                Console.WriteLine("\nUspjesno registrovan, cekam porudzbine");


                var managerRepo = new ManagerRepository(2);
                var managerManagementService = new ManagerManagementService(managerRepo);
                new Thread(() =>
                {
                    managerManagementService.TakeOrReserveATable(managerNumber, Domain.Enums.ClientType.Manager);
                }).Start(); //IsBackground = true ovo ne treba ovde jer sa zatvori klijent onda

                var releaseATableManagerService = new ReleaseATableManagerService(managerRepo, 4001);
                releaseATableManagerService.ReleaseATable(managerNumber); //jako je bitno da ovde i gore bude managerNumber a ne clientId

                //Console.ReadKey();

                new Thread(() =>
                {
                    var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    udpSocket.Bind(new IPEndPoint(IPAddress.Any, 4010));

                    Console.WriteLine("[Manager] Listener za obaveštenja o iskorišćenim rezervacijama pokrenut na portu 4010.");

                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] buffer = new byte[1024];

                    while (true)
                    {
                        try
                        {
                            int received = udpSocket.ReceiveFrom(buffer, ref remote);
                            string message = Encoding.UTF8.GetString(buffer, 0, received).Trim();

                            if (message.StartsWith("RESERVATION_USED;"))
                            {
                                var parts = message.Split(';');
                                if (parts.Length == 2 && int.TryParse(parts[1], out int reservationId))
                                {
                                    Console.WriteLine($"[Manager] Obaveštenje: rezervacija #{reservationId} je iskorišćena.");
                                    managerRepo.RemoveReservation(reservationId);
                                }
                                else
                                {
                                    Console.WriteLine($"[Manager] Neispravna poruka: {message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[Manager] Nepoznata poruka: {message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Manager] Greska u UDP prijemu: {ex.Message}");
                        }
                    }
                })
                { IsBackground = true }.Start();
            }


        }
    }
}
