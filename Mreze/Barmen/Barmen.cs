using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barmen
{
    public class Barmen
    {
        bool busy = false;
        public bool Busy
        {
            get { return busy; }
            set { busy = value; }
        }
        static void Main(string[] args)
        {
            Console.WriteLine($"Barmen number #{args[0]}");
            Console.WriteLine($"ClientId #{args[0]}");
            Console.ReadKey();
        }
    }
}
