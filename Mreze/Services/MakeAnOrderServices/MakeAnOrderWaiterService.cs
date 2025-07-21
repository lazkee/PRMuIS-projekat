using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Domain.Models;
using Domain.Repositories.TableRepository;
using Domain.Repositories.WaiterRepository;
using Domain.Services;

namespace Services.MakeAnOrderServices
{
    public class MakeAnOrderWaiterService : IMakeAnOrder
    {
        IWaiterRepository iWaiterRepository;
        const int serverOrderPort = 15000;
       private Socket _socket;
        public MakeAnOrderWaiterService(IWaiterRepository _iWaiterRepository, int port)
        {
            iWaiterRepository = _iWaiterRepository;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _socket.Connect(new IPEndPoint(IPAddress.Loopback, serverOrderPort));
        }

        private void SendMessage(Socket socket, byte[] message)
        {

            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);
            socket.Send(lengthPrefix);


            socket.Send(message);
        }//mora se prvo slati duzina poruka pa poruka, jer tcp posalje obe poruke odjednom kad saljemo jednu za drugom

        public void MakeAnOrder(int brojSlobodnogStola, int brojGostiju, int WaiterID)
        {
            //Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 15000);

            try
            {

                Console.WriteLine("\n1. Cevapi");
                Console.WriteLine("2. Burek ");
                Console.WriteLine("3. Karadjordjeva");
                Console.WriteLine("4. Pica");
                Console.WriteLine("5. Rakija");
                Console.WriteLine("6. Kisela voda");
                Console.WriteLine("7. Koka kola");
                Console.WriteLine("0. End the order");

                List<Order> orders = new List<Order>();
                int br_narudzbine;

                while (true)
                {

                    Console.Write("Order Something: ");
                    string input = Console.ReadLine();

                    if (int.TryParse(input, out br_narudzbine))
                    {

                        if (br_narudzbine == 0)
                            break;

                        switch (br_narudzbine)
                        {
                            case 1:
                                orders.Add(new Order("Cevapi", ArticleCategory.HRANA, 1200, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 2:
                                orders.Add(new Order("Burek ", ArticleCategory.HRANA, 600, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 3:
                                orders.Add(new Order("Karadjordjeva", ArticleCategory.HRANA, 1350, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 4:
                                orders.Add(new Order("Pica", ArticleCategory.HRANA, 1100, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 5:
                                orders.Add(new Order("Rakija", ArticleCategory.HRANA, 240, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 6:
                                orders.Add(new Order("Kisela voda", ArticleCategory.HRANA, 170, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            case 7:
                                orders.Add(new Order("Koka kola", ArticleCategory.HRANA, 250, ArticleStatus.PRIPREMA, WaiterID, brojSlobodnogStola));
                                break;
                            default:
                                Console.WriteLine("Neispravan unos. Pokusajte ponovo");
                                continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Neispravan unos. Unesite broj.");
                    }
                }

                Console.WriteLine("\nŠta je poručeno:\n");
                Console.WriteLine("| Naziv artikla    | Kategorija artikla     | Cena artikla    |   Status   |");

                foreach (Order order in orders)
                {
                    Console.WriteLine(order);
                }
                
                byte[] tableData;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    
                    Table t = TableRepository.GetByID(brojSlobodnogStola);
                    t.OccupiedBy = WaiterID;
                    t.TableOrders = orders;
                    t.TableState = TableState.BUSY;
                    t.Capacity = brojGostiju;
                    t.TableNumber = brojSlobodnogStola;
                    TableRepository.UpdateTable(t);
                    bf.Serialize(ms, t);
                    tableData = ms.ToArray();
                }

                
                string base64msg = Convert.ToBase64String(tableData);
                string message = $"ORDER;{WaiterID};{brojSlobodnogStola};{base64msg}\n";
                var bytes = Encoding.UTF8.GetBytes(message);
                _socket.Send(bytes);
                Console.WriteLine($"[Waiter] Poslato UDP ORDER: Konobar #{WaiterID}, Broj stola #{brojSlobodnogStola}, Broj artikala:{orders.Count}");


               

                iWaiterRepository.SetWaiterState(WaiterID, false);

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
        }


       








    }
}