using System;

namespace Domain.Models
{
    public enum ArticleCategory { DRINK, FOOD }
    public enum ArticleStatus { INPROGRESS, FINISHED, DELIVERED };

    [Serializable]
    public class Order
    {
        private string _articleName;

        private ArticleCategory _articleCategory;

        private double _price;

        private ArticleStatus _articleStatus;

        public Order(string articleName, ArticleCategory articleCategory, double price, ArticleStatus articleStatus)
        {
            _articleName = articleName;
            _articleCategory = articleCategory;
            _price = price;
            _articleStatus = articleStatus;
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
