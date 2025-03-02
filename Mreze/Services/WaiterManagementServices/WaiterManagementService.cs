using System;
using Domain.Services;

namespace Services.WaiterManagementServices
{
    public class WaiterManagementService : IWaiterManagementService
    {
        ITakeATableClientService iTakeATableClientService;

        public WaiterManagementService(ITakeATableClientService _iTakeATableClientService)
        {
            iTakeATableClientService = _iTakeATableClientService;
        }

        public void WaiterIsServing(int WaiterID)
        {
            bool slobodan = true;
            while (slobodan)
            {

                Console.WriteLine("\n1. Take a new table:");
                Console.WriteLine("0. Close the waiter");
                Console.Write("Your instruction: ");
                string instruction = Console.ReadLine();

                if (int.TryParse(instruction, out int br))
                {
                    switch (br)
                    {

                        case 0:
                            break;
                        //treba taj konobar da se ugasi i izbaci iz svega gde se nalazi, pa ako nema vise konobara da se zavrsi aplikacija
                        //opet ne znam kako bih drugacije ugasio aplikaciju
                        case 1:
                            iTakeATableClientService.TakeATable(WaiterID);
                            break;

                    }
                }
                else
                {
                    Console.WriteLine("You should enter 0 or 1!");
                }

            }
        }
    }
}
