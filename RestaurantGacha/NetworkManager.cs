using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

// ReSharper disable All

namespace RestaurantGacha
{
    public class NetworkManager
    {
        private readonly HttpClient _client = new HttpClient();
        private string _baseUri = "http://192.168.1.230:5001/Restaurant";
        private readonly string _restaurantFile = "Restaurants.txt";
        private readonly string _targetPhysicalAddress = "";
        private DateTime _lastUpdateDateTime;

        public NetworkManager()
        {
            try
            {
                _targetPhysicalAddress = File.ReadAllText("PhysicalAddress.txt");
            }
            catch
            {
            }

            GetTargetIP();
        }

        private void GetTargetIP()
        {
            try
            {
                var result = _client.GetAsync(_baseUri).Result;
            }
            catch
            {
                MessageBox.Show("服务器IP更换或服务器未开启");
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "arp";
                    p.StartInfo.Arguments = "-a";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.Start();

                    string output = p.StandardOutput.ReadToEnd();
                    Regex regex = new Regex($@"([\d\.]+)\s+{_targetPhysicalAddress}");
                    var regexResult = regex.Match(output);
                    if (regexResult.Success)
                    {
                        string target = regexResult.Groups[1].Value;
                        _baseUri = "http://" + target + ":5001/Restaurant";
                        try
                        {
                            var connectAgain = _client.GetAsync(_baseUri).Result;
                        }
                        catch
                        {
                            MessageBox.Show("服务器未开启");
                            _baseUri = "";
                            return;
                        }
                        MessageBox.Show("服务器IP查询成功");
                    }
                    else
                    {
                        MessageBox.Show("服务器IP查询失败，可能不在一个局域网中，或者对方未开机");
                    }
                }
            }
        }

        public ValueTuple<bool, string> GetRestaurants()
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
                        return (true, "更新成功");
                    }
                    else
                    {
                        return (false, "更新失败");
                    }
                }
                catch
                {
                    return (false, "连接服务器失败，服务器未开启");
                }
            }
        }

        public ValueTuple<bool, string> UpdateRestaurants(string restaurantsPath)
        {
            string restaurants = File.ReadAllText(restaurantsPath);

            if (File.Exists(_restaurantFile))
            {
                _lastUpdateDateTime = File.GetLastWriteTime(_restaurantFile);
            }
            var data = new Dictionary<string, string>
            {
                ["preUpdateDateTime"] = _lastUpdateDateTime.ToString(),
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
                    return (true, "更新成功");
                }
                else
                {
                    return (false, "更新失败，餐厅列表文件落后于服务器版本");
                }
            }
            catch
            {
                return (false, "连接服务器失败，服务器未开启");
            }
        }
    }
}
