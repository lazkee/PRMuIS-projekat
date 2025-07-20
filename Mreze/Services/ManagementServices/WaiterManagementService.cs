using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Enums;
using Domain.Repositories.ManagerRepository;
using Domain.Repositories.TableRepository;
using Domain.Repositories.WaiterRepository;
using Domain.Services;
using Domain.Models;

namespace Services.WaiterManagementServices
{
    public class WaiterManagementService : IManagementService
    {
        private readonly ITakeATableClientService _takeTableService;
        private readonly IWaiterRepository _waiterRepo;
        private Socket socket;

        public WaiterManagementService(
            ITakeATableClientService takeTableService,
            IWaiterRepository waiterRepo, 
            int portBill)
        {
            _takeTableService = takeTableService
                ?? throw new ArgumentNullException(nameof(takeTableService));
            _waiterRepo = waiterRepo
                ?? throw new ArgumentNullException(nameof(waiterRepo));
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, portBill));
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 5003));
        }

        public void TakeOrReserveATable(int clientId, ClientType clientType)
        {
            if (clientType == ClientType.Waiter)
            {

                while (!_waiterRepo.HasOrderReady(clientId))
                {
                    
                    Console.WriteLine("1. Zauzmi novi sto:");
                    Console.WriteLine("2. Izdaj racun");
                    Console.WriteLine("0. Zatvori konobara");
                    Console.WriteLine("Izaberi uslugu:");
                    var key = Console.ReadLine();

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
                        Console.WriteLine("Unesite id stola za koji je potreban racun (0 za odustanak): ");
                        int br;
                        bool vrti = true;
                         
                        while (vrti)
                        {
                            bool pokusaj = Int32.TryParse(Console.ReadLine(), out br);
                            if (!pokusaj)
                            {
                                Console.WriteLine("Unesi validan broj stola!");
                            }
                            else if (br == 0)
                            {
                                vrti = false;
                                break;
                            }
                            else if (TableRepository.GetByID(br).TableOrders.Count == 0)
                            {
                                Console.WriteLine("Odabrani sto nema porudzbina ili nije usluzivao odabrani konobar.\nUnesite broj stola za koji je potreban racun(0 za odustanak)\n");
                            }
                            else
                            {
                                
                                socket.Send(Encoding.UTF8.GetBytes($"RACUN;{br.ToString()}"));

                                byte[] buffer = new byte[8192];
                                int bytesRecieved = socket.Receive(buffer);
                                string line = Encoding.UTF8.GetString(buffer, 0, bytesRecieved).Trim();
                                string[] parts = line.Split(';');
                                int iznos = Int32.Parse(parts[0]);

                                
                                //ispis racuna
                                
                                Console.WriteLine($"RACUN ZA STO BROJ {br} ");
                                Console.WriteLine(parts[1]);
                                Console.WriteLine($"UKUPNO:{iznos} ");
                                int baksis= -1;
                                while(baksis < 0 )
                                {
                                    Console.WriteLine("Unesite iznos napojnice (0 ukoliko ne zelite): ");
                                    bool uspjeh = Int32.TryParse(Console.ReadLine(), out  baksis);
                                }
                                

                                int uplaceno=-1;
                                while (uplaceno < iznos)
                                {
                                    Console.WriteLine("Uplaceno: ");
                                    bool uspjeh = Int32.TryParse(Console.ReadLine(), out uplaceno);
                                }
                                

                                // KUSUR;{BRSTOLA};{IZNOS+BAKSIS};UPLACENO;
                                socket.Send(Encoding.UTF8.GetBytes($"KUSUR;{br};{iznos+baksis};{uplaceno}"));
                                byte[] buff = new byte[8192];
                                bytesRecieved = socket.Receive(buff);
                                Console.WriteLine($"{Encoding.UTF8.GetString(buff, 0, bytesRecieved)}");
                                TableRepository.ClearTable(br);
                                vrti = false;
                            }
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
    }
}
