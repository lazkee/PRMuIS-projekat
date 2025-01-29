using System;
using System.Collections.Generic;

namespace Domain.Models
{
    public enum TableState { BUSY, FREE };
    [Serializable]
    public class Table
    {
        private int _tableNumber, _numberOfGuests;
        private TableState _tableState;

        private List<Order> _orders = new List<Order>();

        public int TableNumber
        {
            get { return _tableNumber; }
            set { _tableNumber = value; }
        }

        public int NumberOfGuests
        {
            get { return _numberOfGuests; }
            set { _numberOfGuests = value; }
        }

        public TableState TableState
        {
            get { return _tableState; }
            set { _tableState = value; }
        }

        public Table(int tableNumber, int numberOfGuests, TableState tableState, List<Order> orders)
        {
            _tableNumber = tableNumber;
            _numberOfGuests = numberOfGuests;
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
            ret += "|    Table number    |    Number of guests    |    Table state   |\n";
            ret += $"|    {_tableNumber,-12}    |    {_numberOfGuests,-16}    |    {_tableState,-11}   |\n";
            ret += "------------------------------------------------------------------\n";
            ret += "|\t\t\t     ORDERS\t\t\t\t |\n";
            ret += "------------------------------------------------------------------\n";
            ret += "| Article name   | Article category | Article price |   status   |\n";
            foreach (Order order in _orders)
            {
                ret += order.ToString() + "\n";
            }
            return ret += "------------------------------------------------------------------\n";
        }


    }
}
