using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantGacha
{
    public class NetworkManager
    {
        HttpClient _client=new HttpClient()
        {
            BaseAddress = new Uri("192.168.1.228")
        };
        

        public void UpdateRestaurants()
        {
            //_client.GetAsync(@"")
            _client.BaseAddress= new Uri();
        }
    }
}
