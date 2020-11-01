using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantGacha
{
    public class TextSetting
    {
        public bool IsItaly { get; set; } = false;

        public bool IsBold { get; set; } = false;

        public int FontSize { get; set; } = 14;

        public string FontColor { get; set; } = "#000000";

        public string FontFamily { get; set; }

    }
}
