using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Enums;
using Domain.Repositories.TableRepository;
using Domain.Repositories.WaiterRepository;
using Domain.Services;

namespace Services.WaiterManagementServices
{
    public class WaiterManagementService : IManagementService
    {
        private readonly ITakeATableClientService _takeTableService;
        private readonly IWaiterRepository _waiterRepo;
        private Socket socket;
        private IMakeAnOrder _makeAnOrder;

        public WaiterManagementService(
            ITakeATableClientService takeTableService,
            IWaiterRepository waiterRepo,
            int portBill, IMakeAnOrder makeAnOrder)
        {
            _takeTableService = takeTableService
                ?? throw new ArgumentNullException(nameof(takeTableService));
            _waiterRepo = waiterRepo
                ?? throw new ArgumentNullException(nameof(waiterRepo));
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, portBill));
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5003));
            _makeAnOrder = makeAnOrder;
        }

        public void TakeOrReserveATable(int clientId, ClientType clientType)
        {
            if (clientType == ClientType.Waiter)
            {
                while (!_waiterRepo.HasOrderReady(clientId))
                {

                    Console.WriteLine("\n1. Zauzmi novi sto");
                    Console.WriteLine("2. Izdaj racun");
                    Console.WriteLine("3. Rezervacija");
                    Console.WriteLine("0. Zatvori konobara");
                    Console.Write("Izaberi uslugu: ");
                    var key = Console.ReadLine();
                    //Console.WriteLine($"KEYYYYY {key}\n");

                    //var key = ReadLineWithTimeout(1000 * 10); // 10 seconds timeout
                    /*if (key == null)
                    {
                        Console.WriteLine("Input timed out.");
                        // handle timeout (e.g., repeat menu, default action, etc.)
                    }
                    else
                    {
                        Console.WriteLine($"You entered: {key}");
                    }*/

                    if (key == "1")
                    {
                        // 1) Obeleži konobara kao zauzetog
                        _waiterRepo.SetWaiterState(clientId, true);

                        // 2) Uzmi broj gostiju i pošalji zahtev
                        bool pokusaj = true;

                        while (pokusaj)
                        {
                            Console.Write("Za koliko gostiju je potreban sto: ");
                            if (int.TryParse(Console.ReadLine(), out var numGuests))
                            {
                                pokusaj = false;
                                _takeTableService.TakeATable(clientId, numGuests);
                            }
                            else { Console.WriteLine("Unesi validan broj gostiju."); }

                        }
                    }
                    else if (key == "2")
                    {
                        Console.WriteLine("Unesite id stola za koji je potreban racun: ");
                        int br;
                        bool vrti = true;
                        TableRepository tdb = new TableRepository();
                        while (vrti)
                        {
                            bool pokusaj = Int32.TryParse(Console.ReadLine(), out br);
                            if (!pokusaj)
                            {
                                Console.WriteLine("Unesi validan broj stola!");
                            }
                            else if (tdb.GetByID(br).TableOrders.Count == 0)
                            {
                                Console.WriteLine("Odabrani sto je prazan i nema porudzbina./Unesite broj stola za koji je potreban racun");
                            }
                            else
                            {
                                socket.Send(Encoding.UTF8.GetBytes($"RACUN;{br.ToString()}"));

                                byte[] buffer = new byte[8192];
                                int bytesRecieved = socket.Receive(buffer);
                                string ack = Encoding.UTF8.GetString(buffer, 0, bytesRecieved).Trim();
                                string[] parts = ack.Split(';');
                                bool uspjeh = Int32.TryParse(parts[0], out int iznos);
                                string racun = parts[1];

                                //ispis racuna
                                Console.WriteLine($"Racun za sto broj {br}/n{racun}");

                                Console.WriteLine("Unesite iznos napojnice (0 ukoliko ne zelite): ");
                                uspjeh = Int32.TryParse(Console.ReadLine(), out int baksis);

                                Console.WriteLine("Uplaceno: ");
                                uspjeh = Int32.TryParse(Console.ReadLine(), out int uplaceno);

                                if (uplaceno == baksis + iznos)
                                {
                                    Console.WriteLine("Uplacen je tacan iznos kusur nije potreban.");
                                    socket.Send(Encoding.UTF8.GetBytes($"KUSUR;{br};0"));
                                }
                                else
                                {
                                    Console.WriteLine($"Kusur vracen. Iznos:{uplaceno - baksis - iznos}");
                                    socket.Send(Encoding.UTF8.GetBytes($"KUSUR;{br};0"));
                                }
                            }
                        }
                    }
                    else if (key == "3")
                    {

                        Console.Write("Unesite broj rezervacije: ");
                        string reservationCode = Console.ReadLine();

                        try
                        {
                            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 4003);

                            // Send reservation code to server
                            byte[] dataToSend = Encoding.UTF8.GetBytes(reservationCode);
                            udpSocket.SendTo(dataToSend, serverEndpoint);

                            // Receive response
                            byte[] buffer = new byte[1024];
                            EndPoint fromEndpoint = new IPEndPoint(IPAddress.Any, 0);
                            udpSocket.ReceiveTimeout = 5000; // optional: timeout in ms

                            int receivedBytes = udpSocket.ReceiveFrom(buffer, ref fromEndpoint);
                            string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

                            string[] parts = response.Split(';');
                            if (parts[0] == "OK")
                            {
                                Console.WriteLine($"Rezervacija prihvaćena. Sto broj: {parts[1]}");
                                int.TryParse(parts[1], out int tableNumber);
                                _makeAnOrder.MakeAnOrder(tableNumber, 2, clientId);
                            }
                            else
                            {
                                Console.WriteLine("Neispravan broj rezervacije ili već iskorišćen.");
                            }

                            udpSocket.Close();
                        }
                        catch (SocketException se)
                        {
                            Console.WriteLine("Greška u komunikaciji: " + se.Message);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Došlo je do greške: " + ex.Message);
                        }
                    }
                    else if (key == "0")
                    {
                        Console.WriteLine("Zatvaram konobara…");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unesi 0 ili 1!");
                    }
                }
            }
        }

        public static string ReadLineWithTimeout(int timeoutMs)
        {
            var input = new StringBuilder();
            var watch = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true); // true to not echo
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                    {
                        input.Length--;
                        Console.Write("\b \b"); // erase char from console
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        input.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }
                }
                else
                {
                    if (watch.ElapsedMilliseconds > timeoutMs)
                    {
                        Console.WriteLine();
                        return null; // or empty string on timeout
                    }
                    System.Threading.Thread.Sleep(50); // small delay to reduce CPU usage
                }
            }
            return input.ToString();
        }


    }
}
