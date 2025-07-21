using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Models
{
    public enum TableState { BUSY, FREE };
    [Serializable]
    public class Table
    {
        private int _tableNumber;
        private TableState _tableState;
        private int _occupiedBy;
        private int _capacity;

        private List<Order> _orders = new List<Order>();

        public int TableNumber
        {
            get { return _tableNumber; }
            set { _tableNumber = value; }
        }



        public TableState TableState
        {
            get { return _tableState; }
            set { _tableState = value; }
        }


        public int OccupiedBy
        {
            get { return _occupiedBy; }
            set { _occupiedBy = value; }
        }
        public int Capacity
        {
            get { return _capacity; }
            set { _capacity = value; }
        }


        public Table(int tableNumber, int numberOfGuests, TableState tableState, List<Order> orders)
        {
            _tableNumber = tableNumber;
            _capacity = numberOfGuests;
            _tableState = tableState;
            _orders = orders;



        }
        public List<Order> TableOrders
        {
            get { return _orders; }
            set { _orders = value; }
        }

        public override string ToString()
        {
            string ret = "\n------------------------------------------------------------------\n";
            ret += "|    Broj stola      |    Broj gostiju        |    Stanje stola   |\n";
            ret += $"|    {_tableNumber,-12}    |    {_capacity,-16}    |    {_tableState,-11}   |\n";
            ret += "------------------------------------------------------------------\n";
            ret += "|                            PORUDŽBINE                            |\n";
            ret += "------------------------------------------------------------------\n";
            ret += "| Naziv artikla    | Kategorija artikla     | Cena artikla    |   Status   |\n";

            foreach (Order order in _orders)
            {
                ret += order.ToString() + "\n";
            }
            return ret += "------------------------------------------------------------------\n";
        }


    }
}
