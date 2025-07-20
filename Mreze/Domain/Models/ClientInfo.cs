using System.Net;
using System.Net.Sockets;
using Domain.Enums;

namespace Domain.Models
{
    public class ClientInfo
    {
        public int Id { get; set; }
        public ClientType Type { get; set; }

        public Socket Socket { get; set; }

        public IPEndPoint Endpoint { get; set; }
        
    }
}
