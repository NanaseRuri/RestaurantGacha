using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable All

namespace RestaurantGacha
{
    public class NetworkManager
    {
        private readonly HttpClient _client = new HttpClient();

        private readonly string _baseUri = "http://192.168.1.228:5001/Restaurant";
        //readonly DataContractJsonSerializer _restaurantSerializer = new DataContractJsonSerializer(typeof(List<Restaurant>));
        private readonly string _restaurantFile = "Restaurants.txt";

        public bool GetRestaurants()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(1000))
            {
                try
                {
                    var response = _client.GetAsync(_baseUri + "/GetRestaurants", cts.Token).Result;

                    byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;
                    if (bytes.Length > 0)
                    {
                        using (FileStream fs = new FileStream(_restaurantFile, FileMode.Create))
                        {
                            fs.Write(bytes, 0, bytes.Length);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool UpdateRestaurants(string restaurantsPath)
        {
            string restaurants = File.ReadAllText(restaurantsPath);
            var data = new Dictionary<string, string>
            {
                ["preUpdateDateTime"] = DateTime.Now.ToString(),
                ["restaurants"] = restaurants
            };

            var content = new FormUrlEncodedContent(data);
            try
            {
                var response = _client.PostAsync(_baseUri + "/UpdateRestaurants", content).Result;
                byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;

                string result = Encoding.UTF8.GetString(bytes);
                if (result == "true")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
