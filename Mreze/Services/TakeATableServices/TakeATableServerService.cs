using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Models;
using Domain.Services;

namespace Services.TakeATableServices
{
    public class TakeATableServerService : ITakeATableService
    {
        IReadService _readTablesService;

        public TakeATableServerService(IReadService irs)
        {
            _readTablesService = irs;
        }

        public void TakeATable()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 15001);
            serverSocket.Bind(serverEP);

            serverSocket.Blocking = false;

            EndPoint posiljaocEp = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[1024];


            while (true)
            {
                List<Socket> readSockets = new List<Socket> { serverSocket };

                Socket.Select(readSockets, null, null, 5000000); 

                if (readSockets.Count > 0)
                {
                    try
                    {
                       
                        int bytesReceived = serverSocket.ReceiveFrom(buffer, ref posiljaocEp);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                        //Console.WriteLine($"Received message: {message} from {posiljaocEp}");

                        if (int.TryParse(message, out int numberOfGuests))
                        {
                            //Console.WriteLine($"Parsed number of guests: {numberOfGuests}");
                        }

                        IEnumerable<Table> tables = _readTablesService.GetAllTables();
                        foreach (Table table in tables)
                        {
                            if (table.TableState == TableState.FREE)
                            {
                                
                                string response = table.TableNumber.ToString();
                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                                serverSocket.SendTo(responseBytes, posiljaocEp);

                                table.NumberOfGuests = numberOfGuests;
                                table.TableState = TableState.BUSY;

                                break;
                            }
                            else if (table.TableNumber == 15)
                            {
                                string response = "0";
                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                                serverSocket.SendTo(responseBytes, posiljaocEp);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Error receiving message: {ex.Message}");
                    }
                }
                else
                {
 
                    //Console.WriteLine("Waiting for data...");
                }
            }
        }

    }
}
