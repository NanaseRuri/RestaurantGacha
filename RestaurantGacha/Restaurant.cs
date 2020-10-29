using System;
using System.Collections.Generic;
using System.Text;

namespace RestaurantGacha
{
    public class Restaurant
    {
        public Restaurant()
        {
        }

        public Restaurant(string name, double weight)
        {
            Name = name;
            Weight = weight;
        }

        public string Name
        {
            get;
            set;
        }

        public double Weight { get; set; } = 1;
    }
}
