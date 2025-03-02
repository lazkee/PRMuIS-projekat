﻿using System;
using System.Diagnostics;
using System.IO;
using Domain.Repositories.WaiterRepository;

namespace Domain.Helpers
{
    public class CreateClientInstance
    {
        IWaiterRepository waiterRepository;

        public void BrojITipKlijenta(int brojKlijenata, string tipKlijenta)
        {
            PokreniKlijente(brojKlijenata, tipKlijenta);

            if (tipKlijenta == "konobar")
            {
                waiterRepository = new WaiterRepository(brojKlijenata);
            }
        }

        private void PokreniKlijente(int brojKlijenata, string tipKlijenta)
        {
            string clientPath = "";

            switch (tipKlijenta)
            {

                case "konobar":
                    //clientPath = "D:\\Programi\\Whireshark\\ProjekatSolid\\ProjekatSolid\\Client\\bin\\Debug\\Client.exe";
                    clientPath = Path.Combine("..", "..", "..", "Client", "bin", "Debug", "Client.exe");
                    break;
                case "kuvar":
                    //clientPath = "D:\\Programi\\Whireshark\\ProjekatSolid\\ProjekatSolid\\Cook\\bin\\Debug\\Cook.exe";
                    clientPath = Path.Combine("..", "..", "..", "Cook", "bin", "Debug", "Cook.exe");
                    break;
                case "barmen":
                    clientPath = Path.Combine("..", "..", "..", "Barmen", "bin", "Debug", "Barmen.exe");
                    break;
            }

            for (int i = 0; i < brojKlijenata; i++)
            {
                Process klijentProces = new Process();
                klijentProces.StartInfo.FileName = clientPath;
                klijentProces.StartInfo.Arguments = $"{i + 1} {brojKlijenata}";
                klijentProces.Start();
                Console.WriteLine($"Pokrenut klijent #{i + 1} kao {tipKlijenta}");
            }
        }


    }
}
