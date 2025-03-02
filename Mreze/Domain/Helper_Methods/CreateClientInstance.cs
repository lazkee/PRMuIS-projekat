using System;
using System.Diagnostics;
using System.IO;

namespace Domain.Helper_Methods
{
    public class CreateClientInstance
    {

        public void BrojITipKlijenta()
        {
            Console.Write("Koliko konobara zelite:");
            string broj = Console.ReadLine();
            int.TryParse(broj, out int br_konobara);
            PokreniKlijente("konobar", br_konobara);
            Console.ReadKey();
            //Console.Write("Koliko kuvara zelite:");
            //Console.Write("Koliko barmena zelite:");
        }

        private void PokreniKlijente(string tipOsoblja, int brojKlijenata)
        {
            string clientPath = Path.Combine("..", "..", "..", "Client", "bin", "Debug", "Client.exe");

            if (File.Exists(clientPath))
            {
                // Koristi putanju
            }
            else
            {
                Console.WriteLine("Ne postoji fajl");
                // Hendluj grešku
            }


            for (int i = 0; i < brojKlijenata; i++)
            {

                Process klijentProces = new Process();
                klijentProces.StartInfo.FileName = clientPath;
                klijentProces.StartInfo.Arguments = $"{i + 1} {tipOsoblja}";
                klijentProces.Start();
                //Metoda sa 11. vezbi, samo sto sam uz to dodao i tip osoblja kao arg[1], da se zna koji je tip osoblja (ovo zamenjuje onu klasu Osoblje iz uputstva, valjda sme ovako s obzirom da radimo po solidu)

                //ideja je da kad se udje u klijent, u odnosu na to koji je args[1], se pozivaju servisi za taj tip klijenta 
                Console.WriteLine($"Pokrenut klijent #{i + 1} kao {tipOsoblja}");
            }
        }
    }
}
