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
        UdpClient udpOrderClient = new UdpClient(); 
        public MakeAnOrderWaiterService(IWaiterRepository _iWaiterRepository)
        {
            iWaiterRepository = _iWaiterRepository;
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
                Console.WriteLine("2. Burek sa sirom");
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
                                orders.Add(new Order("Cevapi", ArticleCategory.FOOD, 1200, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 2:
                                orders.Add(new Order("Burek sa sirom", ArticleCategory.FOOD, 600, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 3:
                                orders.Add(new Order("Karadjordjeva", ArticleCategory.FOOD, 1350, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 4:
                                orders.Add(new Order("Pica", ArticleCategory.FOOD, 1100, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 5:
                                orders.Add(new Order("Rakija", ArticleCategory.DRINK, 240, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 6:
                                orders.Add(new Order("Kisela voda", ArticleCategory.DRINK, 170, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            case 7:
                                orders.Add(new Order("Koka kola", ArticleCategory.DRINK, 250, ArticleStatus.INPROGRESS, WaiterID, brojSlobodnogStola));
                                break;
                            default:
                                Console.WriteLine("Invalid instruction. Please try again.");
                                continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid input. Please enter a number.");
                    }
                }

                Console.WriteLine("\nWhat is ordered:\n");
                Console.WriteLine("| Article name   | Article category | Article price |   status   |");
                foreach (Order order in orders)
                {
                    Console.WriteLine(order);
                }
                //clientSocket.Connect(serverEp);
                byte[] tableData;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    ITableRepository tdb = new TableRepository();
                    Table t = tdb.GetByID(brojSlobodnogStola);
                    t.OccupiedBy = WaiterID;
                    t.TableOrders = orders;
                    t.TableState = TableState.BUSY;
                    t.Capacity = brojGostiju;
                    t.TableNumber = brojSlobodnogStola;
                    tdb.updateRepository(t);
                    bf.Serialize(ms, t);
                    tableData = ms.ToArray();
                }
                
                //byte[] waiterIdData = new byte[4];
                //waiterIdData = Encoding.UTF8.GetBytes(WaiterID.ToString());
                //SendMessage(clientSocket, waiterIdData);


                //SendMessage(clientSocket, tableData);
                string base64msg = Convert.ToBase64String(tableData);
                string message = $"ORDER;{WaiterID};{brojSlobodnogStola};{base64msg}\n";
                var bytes = Encoding.UTF8.GetBytes(message);
                udpOrderClient.Send(bytes, bytes.Length, "127.0.0.1", serverOrderPort);
                Console.WriteLine($"[Waiter] Poslato UDP ORDER: Konobar #{WaiterID}, Broj stola #{brojSlobodnogStola}, Broj artikala:{orders.Count}");


                //Console.WriteLine($"\nSuccessfully sent orders and WaiterID: {WaiterID} to the server.");

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
            finally
            {
                //udpOrderClient.Close();
            }
        }


        /*public void MakeAnOrder(int brojSlobodnogStola, int brojGostiju, int WaiterID)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 15000);

            clientSocket.Connect(serverEp);
            //Console.WriteLine("Connected to the server!");
            Console.WriteLine("\n1. Cevapi");
            Console.WriteLine("2. Burek sa sirom");
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
                string poruka = Console.ReadLine();

                if (int.TryParse(poruka, out br_narudzbine))
                {
                    if (br_narudzbine > 0)
                    {
                        switch (br_narudzbine)
                        {
                            case 1:
                                orders.Add(new Order("Cevapi", ArticleCategory.FOOD, 1200, ArticleStatus.INPROGRESS));
                                break;
                            case 2:
                                orders.Add(new Order("Burek sa sirom", ArticleCategory.FOOD, 600, ArticleStatus.INPROGRESS));
                                break;
                            case 3:
                                orders.Add(new Order("Karadjordjeva", ArticleCategory.FOOD, 1350, ArticleStatus.INPROGRESS));
                                break;
                            case 4:
                                orders.Add(new Order("Pica", ArticleCategory.FOOD, 1100, ArticleStatus.INPROGRESS));
                                break;
                            case 5:
                                orders.Add(new Order("Rakija", ArticleCategory.DRINK, 240, ArticleStatus.INPROGRESS));
                                break;
                            case 6:
                                orders.Add(new Order("Kisela voda", ArticleCategory.DRINK, 170, ArticleStatus.INPROGRESS));
                                break;
                            case 7:
                                orders.Add(new Order("Koka kola", ArticleCategory.DRINK, 250, ArticleStatus.INPROGRESS));
                                break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Console.WriteLine("\nWhat is ordered:\n");
            Console.WriteLine("| Article name   | Article category | Article price |   status   |");
            foreach (Order order in orders)
            {
                Console.WriteLine(order);
            }

            //Console.ReadKey();

            byte[] dataBuffer = new byte[1024];
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, new Table(brojSlobodnogStola, brojGostiju, TableState.BUSY, orders));
                    dataBuffer = ms.ToArray();
                }

                clientSocket.Send(dataBuffer);
                //Console.WriteLine($"Successfully sent {dataBuffer.Length} bytes to the server. Table number: {br_narudzbine} number of guests: {brojGostiju}");

               /* dataBuffer = Encoding.UTF8.GetBytes(WaiterID.ToString());
                clientSocket.Send(dataBuffer);
                Console.WriteLine($"Successfully sent {dataBuffer.Length} bytes to the server. WaiterID: {WaiterID} and {dataBuffer}");
                iWaiterRepository.SetWaiterState(WaiterID, false);
                Console.WriteLine($"\nWaiter {WaiterID} is not busy anymore\n");

                iWaiterRepository.SetWaiterState(WaiterID, false);
                Console.WriteLine($"\nWaiter {WaiterID} is not busy anymore\n");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            clientSocket.Close();
            //Console.ReadKey();
        }*/








    }
}