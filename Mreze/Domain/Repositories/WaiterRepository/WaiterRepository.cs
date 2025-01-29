using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Domain.Repositories.WaiterRepository
{
    public class WaiterRepository : IWaiterRepository
    {
        private static Dictionary<int, bool> waiterStates = new Dictionary<int, bool>();
        private static bool initialized = false;
        //private IPEndPoint serverEndpoint;
        //morace vrv da se uradi iskljucivost u ovom repozitorijumu
        public WaiterRepository(int numberofWaiters)
        {
            if (!initialized)
            {
                for (int i = 1; i <= numberofWaiters; ++i)
                {
                    //da bi krenulo od toga da prvi ima 1 a ne 0
                    waiterStates[i] = false; //busy = false
                }
            }
        }

        public void SetWaiterState(int waiterId, bool isBusy)
        {
            waiterStates[waiterId] = isBusy;

        }

        /*private void NotifyServer(int waiterId, bool isBusy)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                string message = $"{waiterId}:{(isBusy ? "busy" : "available")}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                clientSocket.SendTo(messageBytes, serverEndpoint);
            }
        }*/

        public bool GetWaiterState(int waiterId)
        {
            return waiterStates.TryGetValue(waiterId, out bool isBusy) && isBusy;
        }

        public Dictionary<int, bool> GetAllWaiterStates()
        {
            return new Dictionary<int, bool>(waiterStates);

        }


    }
}
