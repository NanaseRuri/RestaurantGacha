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

namespace RestaurantGacha
{
    public class NetworkManager
    {
        private readonly HttpClient _client = new HttpClient();


        private readonly string _baseUri = "http://192.168.1.228:5001/Restaurant";
        readonly DataContractJsonSerializer _restaurantSerializer = new DataContractJsonSerializer(typeof(List<Restaurant>));
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

        public bool UpdateRestaurants(List<Restaurant> restaurants)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"{{\"preUpdateDateTime\":\"{DateTime.Now}\",}},");

            MemoryStream ms = new MemoryStream();
            _restaurantSerializer.WriteObject(ms, restaurants);
            sb.Append("{\"restaurants\":");
            byte[] restaurantsBytes = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(restaurantsBytes, 0, restaurantsBytes.Length);
            string str = Encoding.UTF8.GetString(restaurantsBytes);
            sb.Append(str);
            sb.Append("}");

            sb.Append("}");


            //// 手动将 restaurants JSON 化
            //sb.Append("{\"restaurants\":[");
            //foreach (var restaurant in restaurants)
            //{
            //    sb.Append("{");
            //    sb.Append($"\"Name\":\"{restaurant.Name}\"");
            //    sb.Append($"\"Weight\":\"{restaurant.Weight}\"");
            //    sb.Append("},");
            //}
            //sb.Append("]}");

            HttpContent httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString()));

            var response = _client.PostAsync("UpdateRestaurants", httpContent).Result;
            byte[] bytes = response.Content.ReadAsByteArrayAsync().Result;
            if (bytes[0] == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
