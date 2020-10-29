using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace RestaurantGacha
{
    /// <summary>
    /// 环
    /// </summary>
    public class RingPart
    {
        /// <summary>
        /// 构造函数，构造里外均为圆的圆环
        /// </summary>
        /// <param name="radius">里圆半径</param>
        /// <param name="spanRadius">里外圆半径差</param>
        /// <param name="fillBrush">填充画刷</param>
        public RingPart(double radius, double spanRadius, Brush fillBrush)
        {
            this.RadiusX = radius;
            this.RadiusY = radius;

            this.SpanRadiusX = spanRadius;
            this.SpanRadiusY = spanRadius;

            this.FillBrush = fillBrush;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="radiusX">里边椭圆的长轴</param>
        /// <param name="radiusY">里边椭圆的短轴</param>
        /// <param name="spanRadiusX">里外椭圆的长轴差</param>
        /// <param name="spanRadiusY">里外椭圆的短轴差</param>
        /// <param name="fillBrush">填充画刷</param>
        public RingPart(double radiusX, double radiusY, double spanRadiusX, double spanRadiusY, Brush fillBrush)
        {
            this.RadiusX = radiusX;
            this.RadiusY = radiusY;

            this.SpanRadiusX = spanRadiusX;
            this.SpanRadiusY = spanRadiusY;

            this.FillBrush = fillBrush;
        }

        /// <summary>
        /// 长轴
        /// </summary>
        public double RadiusX { get; set; }

        /// <summary>
        /// 短轴
        /// </summary>
        public double RadiusY { get; set; }

        /// <summary>
        /// 长轴跨越的距离
        /// </summary>
        public double SpanRadiusX { get; set; }

        /// <summary>
        /// 短轴跨越的距离
        /// </summary>
        public double SpanRadiusY { get; set; }

        /// <summary>
        /// 填充画刷
        /// </summary>
        public Brush FillBrush { get; set; }
    }
}
