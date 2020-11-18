using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RestaurantGachaServer.Models;

namespace RestaurantGachaServer.Controllers
{
    public class RestaurantController : Controller
    {
        private DateTime _lastUpdateDateTime = DateTime.Now;
        private const string LoadFile = "Restaurants.txt";

        public string HelloWorld()
        {
            return "Hello World";
        }

        [HttpPost]
        public bool UpdateRestaurants(DateTime preUpdateDateTime, List<Restaurant> restaurants)
        {
            if (_lastUpdateDateTime > preUpdateDateTime)
            {
                return false;
            }
            else
            {
                if (SaveRestaurants(restaurants))
                {
                    _lastUpdateDateTime = preUpdateDateTime;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public FileContentResult GetRestaurants()
        {
            if (System.IO.File.Exists(LoadFile))
            {
                return new FileContentResult(System.IO.File.ReadAllBytes(LoadFile), "txt");
            }
            else
            {
                return null;
            }
        }

        private bool SaveRestaurants(List<Restaurant> restaurants)
        {
            try
            {
                using (var sw = new StreamWriter(LoadFile))
                {
                    foreach (var restaurant in restaurants)
                    {
                        sw.WriteLine(restaurant.Name + ',' + restaurant.Weight);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
