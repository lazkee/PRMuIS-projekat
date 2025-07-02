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
                throw new Exception("Failed to receive message length prefix.");
            }

            int messageLength = BitConverter.ToInt32(lengthPrefix, 0);

            //Console.WriteLine($"\nNew message length: {messageLength}\n");

            byte[] messageData = new byte[messageLength];

            bytesRead = socket.Receive(messageData, 0, messageLength, SocketFlags.None);

            if (bytesRead == 0)
            {
                throw new Exception("Connection closed before message was fully received.");
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
                    Console.WriteLine($"\nWaiterID: {waiterId} sent a new order!");

                    byte[] tableData = ReceiveMessage(clientSocket);
                    using (MemoryStream ms = new MemoryStream(tableData))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        Table table = (Table)bf.Deserialize(ms);
                        Console.WriteLine($"New order is for the table #{table.TableNumber}");

                        IEnumerable<Table> tables = _readTablesService.GetAllTables();
                        foreach (Table t in tables)
                        {
                            if (t.TableNumber == table.TableNumber)
                            {
                                t.TableOrders = table.TableOrders;
                                Console.WriteLine($"Updated table: #{t.TableNumber}\n");
                                Console.WriteLine(t);
                            }
                        }

                        //foreach(Table t in tables)
                        //{
                        //    Console.WriteLine(t);
                        //}

                        // _sendOrderForPreparation.SendOrder(table);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
                finally
                {
                    clientSocket.Close();
                }
            }
        }

    }
}
