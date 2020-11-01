using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RestaurantGacha
{
    public class TurntableSetting
    {
        public double MiddleSpeed { get; set; } = 4;

        public double MaxSpeed { get; set; } = 16;

        public double AccelerateTime { get; set; } = 5;

        public double DecelerateTime { get; set; } = 5;

        public string PointerColor { get; set; } = "#BF3232";

        //public bool RandomEveryTime { get; set; }

        //public bool UseCustomBrush { get; set; }


    }
}
