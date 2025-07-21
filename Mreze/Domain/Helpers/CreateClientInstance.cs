using System;
using System.Diagnostics;
using System.IO;
using Domain.Enums;
using Domain.Repositories.ManagerRepository;
using Domain.Repositories.WaiterRepository;

namespace Server
{
    
    public class CreateClientInstance
    {
        private IWaiterRepository waiterRepository;
        private IManagerRepository managerRepository;
        private int _nextPort = 6000;       // globalni port koji se inkrementira
        private int _nextClientId = 1;

        
        public void BrojITipKlijenta(int brojKlijenata, ClientType tipKlijenta)
        {
            PokreniKlijente(brojKlijenata, tipKlijenta);

            if (tipKlijenta == ClientType.Waiter)
            {
                waiterRepository = new WaiterRepository(brojKlijenata);
            }
            else if (tipKlijenta == ClientType.Manager)
            {
                managerRepository = new ManagerRepository(brojKlijenata);
            }
        }

        
        private void PokreniKlijente(int brojKlijenata, ClientType tipKlijenta)
        {
            
            string clientPath;

            // Odredi putanju do izvršnog fajla klijenta
            switch (tipKlijenta)
            {
                case ClientType.Waiter:
                    clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "Client", "bin", "Debug", "Client.exe");
                    break;
                case ClientType.Cook:
                    clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "Cook", "bin", "Debug", "Cook.exe");
                    break;
                case ClientType.Bartender:
                    clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "Barmen", "bin", "Debug", "Barmen.exe");
                    break;
                case ClientType.Manager:
                    clientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..", "Manager", "bin", "Debug", "Manager.exe");
                    break;
                default:
                    throw new ArgumentException($"Nepoznat tip klijenta: {tipKlijenta}", nameof(tipKlijenta));
            }

            // Provjera da li fajl zaista postoji
            if (!File.Exists(clientPath))
            {
                Console.WriteLine($"Ne mogu pronaći izvršni fajl na {clientPath}");
                return;
            }

            string workingDir = Path.GetDirectoryName(clientPath);
            for (int i = 0; i < brojKlijenata; i++)
            {
                int clientId = _nextClientId++;
                int port = _nextPort++;
                var startInfo = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = $"{clientId} {i + 1} {port}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                Console.WriteLine($"Pokrenut klijent #{clientId} kao {tipKlijenta} na portu {port}");
            }
        }
    }
}
