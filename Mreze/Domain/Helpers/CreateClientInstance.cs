using System;
using System.Diagnostics;
using System.IO;
using Domain.Enums;
using Domain.Repositories;
using Domain.Repositories.WaiterRepository;

namespace Server
{
    /// <summary>
    /// Pokreće instancu navedenog tipa klijenta i inicijalizuje repozitorijum za konobare.
    /// </summary>
    public class CreateClientInstance
    {
        private IWaiterRepository waiterRepository;
        private int _nextPort = 6000;       // globalni port koji se inkrementira
        private int _nextClientId = 1;

        /// <summary>
        /// Pokreće zadati broj ekstenzija klijenta tipa tipKlijenta.
        /// Za konobare inicijalizuje lokalni repository.
        /// </summary>
        /// <param name="brojKlijenata">Broj klijenata koje treba pokrenuti.</param>
        /// <param name="tipKlijenta">Tip klijenta (Waiter, Cook, Bartender).</param>
        public void BrojITipKlijenta(int brojKlijenata, ClientType tipKlijenta)
        {
            PokreniKlijente(brojKlijenata, tipKlijenta);

            if (tipKlijenta == ClientType.Waiter)
            {
                // Inicijalizacija repozitorijuma za praćenje stanja konobara
                waiterRepository = new WaiterRepository(brojKlijenata);
            }
        }

        /// <summary>
        /// Pokretanje procesa za svaki klijent .exe odgovarajućeg tipa i argumenata.
        /// </summary>
        private void PokreniKlijente(int brojKlijenata, ClientType tipKlijenta)
        {
            int startPort = 6000;
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
                    Arguments = $"{clientId} {i+1} {port}",
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                Console.WriteLine($"Pokrenut klijent #{clientId} kao {tipKlijenta} na portu {port}");
            }
        }
    }
}
