using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RestaurantGacha
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:RestaurantGacha"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:RestaurantGacha;assembly=RestaurantGacha"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误:
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:MM/>
    ///
    /// </summary>
    public class Snowflake : Control
    {
        readonly DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        public Snowflake()
        {
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100); ;
            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            _dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var currentColor = Colors.White;
            Brush brush = new RadialGradientBrush(currentColor,
                Color.FromArgb(0, currentColor.R, currentColor.G, currentColor.B));
            Random r = new Random();

            for (int i = 0; i < 500; i++)
            {
                var w = 35 * r.NextDouble();
                var rect =
                    new RectangleGeometry(
                        new Rect(new Point(r.Next(0, (int)this.Width), r.Next(0, (int)this.Height)),
                            new Size(w, w)));
                drawingContext.DrawGeometry(brush, null, rect);
            }
        }
    }
}
