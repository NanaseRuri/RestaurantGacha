using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _loadFile = "Restaurants.txt";
        private readonly ObservableCollection<Restaurant> _restaurants = new ObservableCollection<Restaurant>() { };
        readonly DispatcherTimer _timer = new DispatcherTimer();
        private double _angle = 0;
        private double _maxInterval = 0;
        private double _bezierTime = 0.01;
        readonly List<int> _bezierPoints = new List<int>();
        readonly List<int> _binomialCoefficient = new List<int>();
        private bool _stopClick = false;
        private Grid _lastGrid;
        private Grid _selectedGrid;
        private Grid _preGrid;
        readonly List<Brush> _randomBrushes = new List<Brush>();
        readonly Random _random = new Random((int)DateTime.Now.ToFileTimeUtc());

        public MainWindow()
        {
            InitializeComponent();

            InitialTimer();
            InitialFront();
        }

        void InitialFront()
        {
            InitialBezierPoints();
            InitialBezierWeights();
            CalMaxInterval();
            InitialRestaurantList();
            InitialRandomBrushes();
            DrawRestaurantToGrdPie();
        }

        void GetNextLinearColor(ref int color)
        {
            // 令 temp 为 [-20,20]
            int temp = _random.Next(0, 41);
            temp -= 20;
            if (color + temp > 255 || color + temp < 0)
            {
                color -= temp;
            }
            else
            {
                color += temp;
            }
        }

        void GetNextLinearRgb(ref int red, ref int green, ref int blue)
        {
            GetNextLinearColor(ref red);
            GetNextLinearColor(ref green);
            GetNextLinearColor(ref blue);
        }

        private void InitialRandomBrushes()
        {
            _randomBrushes.Clear();
            int red, green, blue;
            for (int i = 0; i < _restaurants.Count; i++)
            {
                red = _random.Next(0, 255);
                green = _random.Next(0, 255);
                blue = _random.Next(0, 255);
                if (red + green + blue > 690)
                {
                    if (red > 230)
                    {
                        red -= 50;
                    }
                    else if (green > 230)
                    {
                        green -= 50;
                    }
                    else
                    {
                        blue -= 50;
                    }
                }

                GradientStop gradientStop = new GradientStop(Color.FromRgb((byte)red, (byte)green, (byte)blue), 0);

                GetNextLinearRgb(ref red, ref green, ref blue);
                GradientStop gradientStop2 = new GradientStop(Color.FromRgb((byte)red, (byte)green, (byte)blue), 0.3);

                GetNextLinearRgb(ref red, ref green, ref blue);
                GradientStop gradientStop3 = new GradientStop(Color.FromRgb((byte)red, (byte)green, (byte)blue), 6);

                GetNextLinearRgb(ref red, ref green, ref blue);
                GradientStop gradientStop4 = new GradientStop(Color.FromRgb((byte)red, (byte)green, (byte)blue), 1);

                _randomBrushes.Add(new RadialGradientBrush(new GradientStopCollection()
                {
                    gradientStop,
                    gradientStop2,
                    gradientStop3,
                    gradientStop4
                }));
            }
        }

        /// <summary>
        /// 加载餐厅列表，并展示
        /// </summary>
        private void InitialRestaurantList()
        {
            _restaurants.Clear();
            RestaurantList.Children.Clear();

            LoadRestaurant();
            foreach (var restaurant in _restaurants)
            {
                PutRestaurantToFront(restaurant);
            }
        }

        /// <summary>
        /// 展示餐厅到可编辑区域
        /// </summary>
        /// <param name="restaurant"></param>
        void PutRestaurantToFront(Restaurant restaurant)
        {
            TextBox tbName = new TextBox()
            {
                Text = restaurant.Name,
                Width = 150,
                Height = 20,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };
            TextBox tbWeight = new TextBox()
            {
                Text = restaurant.Weight.ToString(),
                Width = 100,
                Height = 20,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            Grid grid = new Grid()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition()
                    {
                        Width = new GridLength(200)
                    },
                    new ColumnDefinition()
                    {
                        Width = new GridLength(150)
                    }
                },
                Tag = restaurant
            };
            grid.Children.Add(tbName);
            grid.Children.Add(tbWeight);
            NameScope.SetNameScope(grid, new NameScope());
            grid.RegisterName("tbName", tbName);
            grid.RegisterName("tbWeight", tbWeight);
            tbName.SetValue(Grid.ColumnProperty, 0);
            tbWeight.SetValue(Grid.ColumnProperty, 1);
            grid.MouseEnter += Grid_MouseEnter;
            grid.MouseLeave += Grid_MouseLeave;
            grid.MouseDown += Grid_MouseDown;
            tbName.GotFocus += TextBox_GotFocus;
            tbName.TextChanged += TbName_TextChanged;
            tbWeight.GotFocus += TextBox_GotFocus;
            tbWeight.TextChanged += TbWeight_TextChanged;

            RestaurantList.Children.Add(grid);
        }

        private void TbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Parent is Grid d)
            {
                ((Restaurant)d.Tag).Name = ((TextBox)sender).Text;
            }
        }

        private void TbWeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Parent is Grid d)
            {
                double weight;
                try
                {
                    weight = double.Parse(((TextBox)sender).Text);
                }
                catch
                {
                    weight = 1;
                }

                ((Restaurant)d.Tag).Weight = weight;
            }
        }

        /// <summary>
        /// 获取焦点时令之前选中的框背景色变回默认颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).Parent is Grid d)
            {
                _selectedGrid = d;
                CheckPreGrid();

                _preGrid = _selectedGrid;
                _selectedGrid.Background = new SolidColorBrush(Colors.Bisque);
            }
        }

        /// <summary>
        /// 变色
        /// </summary>
        void CheckPreGrid()
        {
            if (_preGrid != null && _preGrid != _selectedGrid)
            {
                if (_preGrid?.Tag is Restaurant r)
                {
                    TextBox tbName = (TextBox)_preGrid.FindName("tbName");
                    TextBox tbWeight = (TextBox)_preGrid.FindName("tbWeight");
                    r.Name = tbName.Text;
                    try
                    {
                        r.Weight = double.Parse(tbWeight.Text);
                    }
                    catch
                    {
                        r.Weight = 1;
                    }
                    if (string.IsNullOrEmpty(r.Name))
                    {
                        _restaurants.Remove(r);
                        RestaurantList.Children.Remove(_preGrid);
                    }
                }

                if (_preGrid != null)
                {
                    _preGrid.Background = new SolidColorBrush();
                }
            }
        }

        /// <summary>
        /// 点击时作为选中框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _selectedGrid = (Grid)sender;
            CheckPreGrid();

            _preGrid = _selectedGrid;
            _selectedGrid.Background = new SolidColorBrush(Colors.Bisque);
        }

        /// <summary>
        /// 鼠标离开时变色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            _lastGrid = (Grid)sender;
        }

        /// <summary>
        /// 鼠标进入时变色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_lastGrid != null && _lastGrid != sender)
            {
                _lastGrid.Background = new SolidColorBrush();
            }
            ((Grid)sender).Background = new SolidColorBrush(Colors.DarkGray);

            if (_selectedGrid != null)
            {
                _selectedGrid.Background = new SolidColorBrush(Colors.Bisque);
            }
        }

        /// <summary>
        /// 计算最大的转速
        /// </summary>
        private void CalMaxInterval()
        {
            _maxInterval = 0;
            for (int i = 0; i < _bezierPoints.Count; i++)
            {
                _maxInterval = _maxInterval + Math.Pow(0.5, _bezierPoints.Count - i) * Math.Pow((1 - 0.5), i) * _binomialCoefficient[i] * _bezierPoints[i];
            }
        }

        /// <summary>
        /// 获取各个项的牛顿系数
        /// </summary>
        private void InitialBezierWeights()
        {
            _binomialCoefficient.Clear();
            for (int i = 0; i < _bezierPoints.Count; i++)
            {
                _binomialCoefficient.Add(GetBinomialCoefficient(i, _bezierPoints.Count));
            }
        }

        /// <summary>
        /// 计算牛顿系数
        /// </summary>
        int GetBinomialCoefficient(int index, int power)
        {
            if (index == 0 || index == power)
            {
                return 1;
            }

            if (index > power / 2)
            {
                index = power / 2;
            }

            int multiple = 1;
            for (; index > 0; index--, power--)
            {
                multiple *= power;
            }

            return multiple;
        }

        /// <summary>
        /// 获得当前的转速
        /// </summary>
        /// <returns></returns>
        double GetSpeed()
        {
            double result = 0;
            //停止按钮按下时，对称过去计算
            if (_stopClick && _bezierTime < 0.5)
            {
                for (int i = 0; i < _bezierPoints.Count; i++)
                {
                    result = result + Math.Pow(_bezierTime, _bezierPoints.Count - i) * Math.Pow((1 - _bezierTime), i) * _binomialCoefficient[i] * _bezierPoints[i];
                }
            }
            else
            {
                for (int i = 0; i < _bezierPoints.Count; i++)
                {
                    result = result + Math.Pow(_bezierTime, i) * Math.Pow((1 - _bezierTime), _bezierPoints.Count - i) * _binomialCoefficient[i] * _bezierPoints[i];
                }
            }
            return result;
        }

        /// <summary>
        /// 初始化各个点的值
        /// </summary>
        private void InitialBezierPoints()
        {
            _bezierPoints.Clear();
            _bezierPoints.Add(4);
            _bezierPoints.Add(16);
            _bezierPoints.Add(16);
            _bezierPoints.Add(4);
        }

        private void InitialTimer()
        {
            _timer.Tick += _timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(10);
        }

        /// <summary>
        /// 设置不同时候的按钮状态
        /// </summary>
        /// <param name="stopped"></param>
        void SetButtonState(bool stopped)
        {
            if (stopped)
            {
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
            }
            else
            {
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
            }
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (!_stopClick)
            {
                if (_bezierTime < 0.5)
                {
                    _angle += GetSpeed();
                    _bezierTime += 0.0015;
                }
                else
                {
                    _angle += _maxInterval;
                }
                if (_bezierTime >= 1)
                {
                    _timer.Stop();
                    SetButtonState(true);
                }
            }
            else
            {
                _angle += GetSpeed();
                _bezierTime -= 0.001;
                if (_bezierTime <= 0)
                {
                    _timer.Stop();
                    SetButtonState(true);
                }
            }
            if (_angle > 360)
            {
                _angle -= 360;
            }

            RotateTransform.Angle = _angle;
        }


        void DrawRestaurantToGrdPie()
        {
            GrdPie.Children.Clear();

            double totalWeight = 0;

            foreach (var restaurant in _restaurants)
            {
                totalWeight += restaurant.Weight;
            }

            var sectorParts = new List<SectorPart>();

            int index = 0;
            foreach (var restaurant in _restaurants)
            {
                SectorPart sectorPart = new SectorPart(restaurant.Weight / totalWeight * 360, _randomBrushes[index]);
                sectorParts.Add(sectorPart);
                index++;
            }

            //var ringParts = new List<RingPart>();
            //ringParts.Add(new RingPart(40, 20, 40, 20, Brushes.White));

            Point midPoint = new Point(300, 300);
            //var shapes = PieChartDrawer.GetEllipsePieChartShapes(midPoint, 100, 100, 30, sectorParts, ringParts);
            var shapes = PieChartDrawer.GetEllipsePieChartShapes(midPoint, 200, 200, 30, sectorParts);
            foreach (var shape in shapes)
            {
                GrdPie.Children.Add(shape);
            }
            SetEllipsePieChartLabel(midPoint, 200, 200, 30, sectorParts, _restaurants.ToList());
        }

        void SetEllipsePieChartLabel(Point center, double radiusX, double radiusY, double offsetAngle, IEnumerable<SectorPart> sectorParts, List<Restaurant> restaurants)
        {
            double startAngle = offsetAngle;

            int index = 0;
            foreach (var sectorPart in sectorParts)
            {
                // 分两轮加，使文本位置在中间
                startAngle += Math.Round(sectorPart.SpanAngle / 2);

                // 扇形顺时针方向在椭圆上的第一个点
                var firstPoint = center.GetEllipsePoint(radiusX / 2, radiusY / 2, startAngle);
                Label label = new Label()
                {
                    Content = restaurants[index].Name,
                    Margin = new Thickness(firstPoint.X - 15 - restaurants[index].Name.Length * 3, firstPoint.Y - 15, 0, 0),
                    FontFamily = new FontFamily("NSimSun"),
                    FontWeight = FontWeights.Bold,
                    FontSize = 15
                };
                GrdPie.Children.Add(label);

                startAngle += Math.Ceiling(sectorPart.SpanAngle / 2);
                index++;
            }
        }

        private void SaveRestaurants()
        {
            try
            {
                using (var sw = new StreamWriter(_loadFile))
                {
                    foreach (var restaurant in _restaurants)
                    {
                        sw.WriteLine(restaurant.Name + ',' + restaurant.Weight);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        void LoadRestaurant()
        {
            if (File.Exists(_loadFile))
            {
                string[] lines = File.ReadAllLines(_loadFile);
                foreach (var line in lines)
                {
                    string[] temp = line.Split(',');

                    Restaurant restaurant = new Restaurant
                    {
                        Name = temp[0]
                    };
                    double weight = 1;
                    try
                    {
                        weight = double.Parse(temp[1]);
                    }
                    catch
                    {
                        // ignored
                    }

                    restaurant.Weight = weight;
                    _restaurants.Add(restaurant);
                }
            }
        }

        private void RemoveItem(object sender, RoutedEventArgs e)
        {
            if (_selectedGrid?.Tag != null)
            {
                _restaurants.Remove((Restaurant)_selectedGrid.Tag);
                RestaurantList.Children.Remove(_selectedGrid);
            }
        }

        private void AddRestaurant(object sender, RoutedEventArgs e)
        {
            Restaurant restaurant = new Restaurant();
            _restaurants.Add(restaurant);

            PutRestaurantToFront(restaurant);
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            _stopClick = false;
            _timer.Start();

            SetButtonState(false);
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            _stopClick = true;
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            _stopClick = false;
            _timer.Stop();
            RotateTransform.Angle = 0;
            _angle = 0;
            _bezierTime = 0;

            SetButtonState(true);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveRestaurants();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Turntable.IsSelected)
            {
                SaveRestaurants();

                if (Turntable.IsSelected)
                {
                    InitialFront();
                }
            }
            else
            {
                _timer.Stop();
            }
        }
    }
}
