using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Domain.Models;
using Domain.Services;

namespace Services.TakeOrdersFromWaiterServices
{
    public class TakeOrdersFromWaiterService : ITakeOrdersFromWaiterService
    {
        IReadService _readTablesService;
        ISendOrderForPreparation _sendOrderForPreparation;

        public TakeOrdersFromWaiterService(IReadService readTablesService, ISendOrderForPreparation sendOrderForPreparation)
        {
            _readTablesService = readTablesService;
            _sendOrderForPreparation = sendOrderForPreparation;
        }

        private byte[] ReceiveMessage(Socket socket)
        {

            byte[] lengthPrefix = new byte[4];
            int bytesRead = socket.Receive(lengthPrefix, 0, 4, SocketFlags.None);

            if (bytesRead < 4)
            {
                throw new Exception("Greska pri primanju duzine prefiksa poruke.");
            }

            int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

            byte[] messageData = new byte[messageLength];

            bytesRead = socket.Receive(messageData, 0, messageLength, SocketFlags.None);

            if (bytesRead == 0)
            {
                throw new Exception("Prekinuta konekcija pre primanja cele poruke.");
            }

            return messageData;
        }

        public void ProcessAnOrder()
        {
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, 15000);

            listenSocket.Bind(serverEp);
            listenSocket.Listen(10);

            while (true)
            {
                Socket clientSocket = listenSocket.Accept();

                try
                {

                    byte[] waiterIdData = ReceiveMessage(clientSocket);
                    string waiterIdString = Encoding.UTF8.GetString(waiterIdData);
                    int waiterId = int.Parse(waiterIdString);
                    Console.WriteLine($"\nKonobar #{waiterId} poslao novu porudzbinu!");

                    byte[] tableData = ReceiveMessage(clientSocket);
                    using (MemoryStream ms = new MemoryStream(tableData))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        Table table = (Table)bf.Deserialize(ms);
                        Console.WriteLine($"Nova porudzbina je za sto #{table.TableNumber}");

                        IEnumerable<Table> tables = _readTablesService.GetAllTables();
                        foreach (Table t in tables)
                        {
                            if (t.TableNumber == table.TableNumber)
                            {
                                t.TableOrders = table.TableOrders;
                                Console.WriteLine($"Azuriran sto broj #{t.TableNumber}\n");
                                Console.WriteLine(t);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska pri procesuiranju poruke: {ex.Message}");
                }
                finally
                {
                    clientSocket.Close();
                }
            }
        }
    }
}
