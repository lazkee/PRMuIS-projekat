using System;

namespace Domain.Helper_Methods
{
    public class CreateClientCheck
    {

        public string CheckClientType(string clientNumber, string clientType)
        {

            int brojKlijenta = 0;
            if ((brojKlijenta = int.Parse(clientNumber)) == 0)
            {
                Console.WriteLine("Invalid Client number!");
                return "invalid";
            }
            string tipOsoblja = clientType;

            if (clientType != "konobar" && clientType != "barmen" && clientType != "kuvar")
            {
                Console.WriteLine("Invalid Client type!");
                return "invalid";
            }
            else
            {
                Console.WriteLine($"Klijent #{brojKlijenta} pokrenut kao {tipOsoblja}");
                return tipOsoblja;
            }
        }
    }
}
