using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantGachaServer.Models
{
    public class Restaurant
    {
        public string Name
        {
            get;
            set;
        }

        public double Weight { get; set; } = 1;
    }
}
