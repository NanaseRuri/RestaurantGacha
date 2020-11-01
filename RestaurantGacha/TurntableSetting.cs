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
        public double StartSpeed { get; set; }

        public double MaxSpeed { get; set; }

        public double EndSpeed { get; set; }

        public double AccelerateTime { get; set; }

        public double DecelerateTime { get; set; }

        //public bool RandomEveryTime { get; set; }

        //public bool UseCustomBrush { get; set; }


    }
}
