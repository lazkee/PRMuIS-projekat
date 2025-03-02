using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain.Services;

namespace Services.TakeATableServices
{
    public class TakeATableClientService : ITakeATableService
    {

        IMakeAnOrder iMakeAnOrderWaiterService;

        public TakeATableClientService(IMakeAnOrder _iMakeAnOrderWaiterService) {
            iMakeAnOrderWaiterService = _iMakeAnOrderWaiterService;
        }

        public void TakeATable()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint serverEp = new IPEndPoint(IPAddress.Loopback, 15001);

            Console.Write("How many guests per table:");

            string poruka = Console.ReadLine();
            byte[] binarnaPoruka = Encoding.UTF8.GetBytes(poruka);

            int.TryParse(poruka, out int broj_gostiju);

            try
            {
                int brBajta = clientSocket.SendTo(binarnaPoruka, 0, binarnaPoruka.Length, SocketFlags.None, serverEp);

                //Console.WriteLine($"Uspesno poslato {brBajta} bajta ka {serverEp}");

                byte[] buffer = new byte[1024];

                EndPoint posiljaocEp = new IPEndPoint(IPAddress.Any, 0);

                int brBajta1 = clientSocket.ReceiveFrom(buffer, ref posiljaocEp);

                string poruka1 = Encoding.UTF8.GetString(buffer, 0, brBajta1);

                //Console.WriteLine($"Stiglo je {brBajta1} od {posiljaocEp}, poruka: {poruka1}");

                if (int.TryParse(poruka1, out int br1))
                {
                    if (br1 == 0)
                    {
                        Console.WriteLine("Trenutno nema slobodnih stolova!");
                    }
                    else
                    {
                        Console.WriteLine($"Sto broj {br1} je slobodan!");
                        iMakeAnOrderWaiterService.MakeAnOrder(br1, broj_gostiju);
                        //MakeAnOrder(br1, broj_gostiju);
                    }
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine
                    (e.ToString());
            }

            //Console.WriteLine("Table reserved!");
            clientSocket.Close();
        }
    }
}
