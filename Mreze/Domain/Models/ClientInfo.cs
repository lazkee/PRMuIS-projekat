using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Models
{
    public class ClientInfo
    {
        public int Id { get; set; }
        public ClientType Type{get; set;}

        public TcpClient Socket { get; set; }
        
        public IPEndPoint UdpEndpoint { get; set; }
    }
}
