using System;

namespace Cook
{
    public class Cook
    {
        bool busy = false;
        public bool Busy
        {
            get { return busy; }
            set { busy = value; }
        }
        static void Main(string[] args)
        {
            Console.WriteLine($"Cook number #{args[0]}");
            Console.WriteLine($"ClientId #{args[0]}");

            Console.ReadKey();
        }
    }
}
