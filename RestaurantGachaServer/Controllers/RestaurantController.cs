using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;

namespace RestaurantGachaServer.Controllers
{
    public class RestaurantController : Controller
    {
        private const string LoadFile = "Restaurants.txt";
        readonly Mutex _mutex = new Mutex();

        public string HelloWorld()
        {
            return "Hello World";
        }

        [HttpPost]
        public bool UpdateRestaurants(DateTime preUpdateDateTime, string restaurants)
        {
            DateTime lastUpdateDateTime = System.IO.File.GetLastWriteTime(LoadFile);
            if (Monitor.TryEnter(_mutex, 2000))
            {
                if (lastUpdateDateTime > preUpdateDateTime)
                {
                    Monitor.Exit(_mutex);
                    return false;
                }
                else
                {
                    try
                    {
                        System.IO.File.WriteAllText(LoadFile, restaurants);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                    finally
                    {
                        Monitor.Exit(_mutex);
                    }
                }
            }
            else
            {
                return false;
            }
        }

        public FileContentResult GetRestaurants()
        {
            if (System.IO.File.Exists(LoadFile))
            {
                return new FileContentResult(System.IO.File.ReadAllBytes(LoadFile), "text/plain");
            }
            else
            {
                return null;
            }
        }
    }
}
