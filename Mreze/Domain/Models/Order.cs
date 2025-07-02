using System;

namespace Domain.Models
{
    public enum ArticleCategory { DRINK, FOOD }
    public enum ArticleStatus { INPROGRESS, FINISHED, DELIVERED };

    [Serializable]
    public class Order
    {
        public string _articleName;

        public ArticleCategory _articleCategory;

        public double _price;

        public ArticleStatus _articleStatus;

        public int _waiterId { get; set; }
        public int _tableNumber { get; set; }
        public Order(string articleName, ArticleCategory articleCategory, double price, ArticleStatus articleStatus,int waiterId, int tableNumber)
        {
            _articleName = articleName;
            _articleCategory = articleCategory;
            _price = price;
            _articleStatus = articleStatus;
            _waiterId = waiterId;
            _tableNumber = tableNumber;
        }
        public ArticleCategory ArticleCategory
        {
            get { return _articleCategory; }
            set { _articleCategory = value; }
        }

        public override string ToString()
        {
            //string ret = "| Article name   | Article category | Article price |   status   |\n";
            return $"| {_articleName,-14} | {_articleCategory,-16} | {_price,-13} | {_articleStatus,-10} |";
        }

    }
}
