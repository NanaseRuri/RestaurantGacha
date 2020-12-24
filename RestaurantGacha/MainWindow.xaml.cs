using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
// ReSharper disable All

namespace RestaurantGacha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //餐厅名单
        string _loadFile = "Restaurants.txt";
        private readonly ObservableCollection<Restaurant> _restaurants = new ObservableCollection<Restaurant>();
        //转盘设置
        TurntableSetting _turntableSetting = new TurntableSetting();
        private string _turntableSettingFile = "TurntableSetting.config";
        //文本设置
        TextSetting _textSetting = new TextSetting();
        private string _textSettingFile = "TextSetting.config";

        //支持的字体
        readonly List<FontFamilyInfo> _fontFamilyInfos = new List<FontFamilyInfo>();

        //定时器，用于进行画面绘制
        //readonly DispatcherTimer _timer = new DispatcherTimer();
        readonly Timer _timer = new Timer(10);

        //定时器间隔
        private int _timerInterval = 10;
        //当按下停止键时，按照现有速度继续转一定时间
        private int _randomDelay = -1;
        //当前转角
        private double _angle = 0;
        //最大转速
        private double _maxAngleInterval = 0;
        //当前转速
        private double _currentAngleInterval = 0;
        //已进行旋转的时间
        private double _bezierTime = 0;
        //加速的时间间隔
        private double _bezierAcceInterval = 0;
        //减速的时间间隔
        private double _bezierDeceInterval = 0;
        //贝塞尔曲线的速度点
        readonly List<double> _bezierPoints = new List<double>();
        //牛顿系数
        readonly List<int> _binomialCoefficient = new List<int>();
        //停止键是否按下
        private bool _stopClick = false;

        //编辑餐厅页面手绘，通过这些元素触发事件
        private Grid _lastGrid;
        private Grid _selectedGrid;
        private Grid _preGrid;

        //全局的随机数生成器
        readonly Random _random = new Random((int)DateTime.Now.ToFileTimeUtc());
        //转盘的各个餐厅的颜色
        readonly List<Brush> _randomBrushes = new List<Brush>();

        NetworkManager _networkManager = new NetworkManager();

        public MainWindow()
        {
            InitializeComponent();

            LoadAllFiles();
            InitialSettings();
            InitialBezier();
            InitialTimer();
            InitialFront();

            if (DateTime.Now.Month != 12 || (DateTime.Now.Day != 24 && DateTime.Now.Day != 25))
            {
                ControlButtonPanel.Children.Remove(ChristmasTree);
            }
        }

        //单纯的将变量的值赋给页面上的元素
        void InitialSettings()
        {
            InitialFontFamilies();

            MiddleSpeed.Text = _turntableSetting.MiddleSpeed.ToString();
            MaxSpeed.Text = _turntableSetting.MaxSpeed.ToString();
            AccelerateTime.Text = _turntableSetting.AccelerateTime.ToString();
            DecelerateTime.Text = _turntableSetting.DecelerateTime.ToString();

            IsItaly.IsChecked = _textSetting.IsItaly;
            IsBold.IsChecked = _textSetting.IsBold;
            CustomFontSize.Text = _textSetting.FontSize.ToString();
            CustomFontColor.Text = _textSetting.FontColor;

            CustomFontFamily.ItemsSource = _fontFamilyInfos;
            CustomFontFamily.DisplayMemberPath = "FontFamilyName";
            int index = 0;
            foreach (var info in _fontFamilyInfos)
            {
                if (info.FontFamilyName == _textSetting.FontFamily)
                {
                    break;
                }
                index++;
            }
            if (index < _fontFamilyInfos.Count)
            {
                CustomFontFamily.SelectedIndex = index;
            }
        }

        //动态地获取存在的字库
        void InitialFontFamilies()
        {
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                LanguageSpecificStringDictionary fontdics = fontFamily.FamilyNames;
                //判断该字体是不是中文字体
                if (fontdics.ContainsKey(XmlLanguage.GetLanguage("zh-cn")))
                {
                    if (fontdics.TryGetValue(XmlLanguage.GetLanguage("zh-cn"), out string fontFamilyName))
                    {
                        _fontFamilyInfos.Add(new FontFamilyInfo()
                        {
                            FontFamily = fontFamily,
                            FontFamilyName = fontFamilyName
                        });
                    }
                }
                //英文字体
                else
                {
                    if (fontdics.TryGetValue(XmlLanguage.GetLanguage("en-us"), out string fontFamilyName))
                    {
                        _fontFamilyInfos.Add(new FontFamilyInfo()
                        {
                            FontFamily = fontFamily,
                            FontFamilyName = fontFamilyName
                        });
                    }
                }
            }
        }

        //有文件的就进行读取
        void LoadAllFiles()
        {
            LoadRestaurant();
            LoadTurntableSetting();
            LoadTextSetting();
        }

        //初始化贝塞尔相关的
        void InitialBezier()
        {
            InitialBezierPoints();
            InitialBezierWeights();
            CalMaxInterval();
            CalBezierIntervalTime();
        }

        //通过加速时间和减速时间计算时间间隔
        void CalBezierIntervalTime()
        {
            _bezierAcceInterval = 0.5 * _timerInterval / 1000 / _turntableSetting.AccelerateTime;
            _bezierDeceInterval = 0.5 * _timerInterval / 1000 / _turntableSetting.DecelerateTime;
        }

        //通过变量初始化编辑餐厅页面，随机初始化转盘的颜色，并绘制转盘
        void InitialFront()
        {
            InitialRestaurantList();
            InitialRandomBrushes();
            DrawRestaurantToGrdPie();
        }

        //获取单个随机的颜色
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

        //返回RGB
        void GetNextLinearRgb(ref int red, ref int green, ref int blue)
        {
            GetNextLinearColor(ref red);
            GetNextLinearColor(ref green);
            GetNextLinearColor(ref blue);
        }

        //获取转盘各个部分的颜色
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
            //_restaurants.Clear();
            RestaurantList.Children.Clear();

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
                Height = 24,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5),
                FontSize = 18
            };
            TextBox tbWeight = new TextBox()
            {
                Text = restaurant.Weight.ToString(),
                Width = 100,
                Height = 24,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5),
                FontSize = 18
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

        //同步
        private void TbName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Parent is Grid d)
            {
                ((Restaurant)d.Tag).Name = ((TextBox)sender).Text;
            }
        }

        //同步
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
            _maxAngleInterval = 0;
            for (int i = 0; i < _bezierPoints.Count; i++)
            {
                _maxAngleInterval = _maxAngleInterval + Math.Pow(0.5, _bezierPoints.Count - i) * Math.Pow((1 - 0.5), i) * _binomialCoefficient[i] * _bezierPoints[i];
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
            if (index == 0 || index == power - 1)
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
            if (_stopClick)
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

            _bezierPoints.Add(0);
            _bezierPoints.Add(_turntableSetting.MiddleSpeed);
            _bezierPoints.Add(_turntableSetting.MaxSpeed);
            _bezierPoints.Add(_turntableSetting.MiddleSpeed);
            _bezierPoints.Add(0);
        }

        private void InitialTimer()
        {
            //_timer.Tick += _timer_Tick;
            //_timer.Interval = TimeSpan.FromMilliseconds(_timerInterval);
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(_timer_Tick);
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


        private void _timer_Tick()
        {
            if (!_stopClick)
            {
                if (_bezierTime < 0.5)
                {
                    _currentAngleInterval = GetSpeed();
                    _angle += _currentAngleInterval;
                    _bezierTime += _bezierAcceInterval;
                }
                else
                {
                    _angle += _maxAngleInterval;
                }
                //if (_bezierTime >= 1)
                //{
                //    _timer.Stop();
                //    SetButtonState(true);
                //}
            }
            else
            {
                if (_randomDelay > -1)
                {
                    _angle += _currentAngleInterval;
                    _randomDelay--;
                }
                else
                {
                    _currentAngleInterval = GetSpeed();
                    _angle += _currentAngleInterval;
                    _bezierTime -= _bezierDeceInterval;
                    if (_bezierTime <= 0)
                    {
                        _timer.Stop();
                        SetButtonState(true);
                    }
                }
            }
            if (_angle > 360)
            {
                _angle -= 360;
            }

            RotateTransform.Angle = _angle;
        }

        //将餐厅绘制在饼状图上
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

            Point midPoint = new Point(400, 400);
            //var shapes = PieChartDrawer.GetEllipsePieChartShapes(midPoint, 100, 100, 30, sectorParts, ringParts);
            var shapes = PieChartDrawer.GetEllipsePieChartShapes(midPoint, 300, 300, 30, sectorParts);
            foreach (var shape in shapes)
            {
                GrdPie.Children.Add(shape);
            }
            SetEllipsePieChartLabel(midPoint, 300, 300, 30, sectorParts, _restaurants.ToList());
        }

        //添加文本
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
                    Content = restaurants[index].Name ?? "",
                    Margin = new Thickness(firstPoint.X - 22 - (double)restaurants[index].Name.Length / 4 * _textSetting.FontSize, firstPoint.Y - 22, 0, 0),
                    FontFamily = _fontFamilyInfos.FirstOrDefault(info => info.FontFamilyName == _textSetting.FontFamily) == null ? new FontFamily() :
                        _fontFamilyInfos.First(info => info.FontFamilyName == _textSetting.FontFamily).FontFamily,
                    FontWeight = _textSetting.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontSize = _textSetting.FontSize,
                    FontStyle = _textSetting.IsItaly ? FontStyles.Italic : FontStyles.Normal,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_textSetting.FontColor))
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
            _randomDelay = _random.Next(50, 150);
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
            _timer.Stop();
            SaveRestaurants();
            SaveTurntableSetting();
            SaveTextSetting();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!OtherSettings.IsSelected)
            {
                SetOtherSettings();
            }

            if (Turntable.IsSelected)
            {
                Turntable.Focus();
                InitialBezier();
                InitialFront();
            }
            else
            {
                _timer.Stop();
                SetButtonState(true);
            }
        }

        void SetOtherSettings()
        {
            try
            {
                _turntableSetting.MiddleSpeed = double.Parse(MiddleSpeed.Text);
            }
            catch
            {
                _turntableSetting.MiddleSpeed = 4;
                MiddleSpeed.Text = 4.ToString();
            }

            try
            {
                _turntableSetting.MaxSpeed = double.Parse(MaxSpeed.Text);
            }
            catch
            {
                _turntableSetting.MaxSpeed = 16;
                MaxSpeed.Text = 16.ToString();
            }

            try
            {
                _turntableSetting.AccelerateTime = double.Parse(AccelerateTime.Text);
            }
            catch
            {
                _turntableSetting.AccelerateTime = 5;
                AccelerateTime.Text = 5.ToString();
            }

            try
            {
                _turntableSetting.DecelerateTime = double.Parse(DecelerateTime.Text);
            }
            catch
            {
                _turntableSetting.DecelerateTime = 5;
                DecelerateTime.Text = 5.ToString();
            }

            if (ColorConverter.ConvertFromString(PointerColor.Text) != null)
            {
                _turntableSetting.PointerColor = PointerColor.Text;
            }
            else
            {
                _turntableSetting.PointerColor = "#BF3232";
                PointerColor.Text = "#BF3232";
            }

            _textSetting.IsItaly = IsItaly.IsChecked.HasValue ? IsItaly.IsChecked.Value : false;
            _textSetting.IsBold = IsBold.IsChecked.HasValue ? IsBold.IsChecked.Value : false;


            try
            {
                _textSetting.FontSize = int.Parse(CustomFontSize.Text);
            }
            catch
            {
                _textSetting.FontSize = 14;
                CustomFontSize.Text = 14.ToString();
            }

            if (ColorConverter.ConvertFromString(CustomFontColor.Text) != null)
            {
                _textSetting.FontColor = CustomFontColor.Text;
            }
            else
            {
                _textSetting.FontColor = "#000000";
                CustomFontColor.Text = "#000000";
            }

            if (CustomFontFamily.SelectedIndex > -1)
            {
                _textSetting.FontFamily = _fontFamilyInfos[CustomFontFamily.SelectedIndex].FontFamilyName;
            }
        }

        private void SaveTurntableSetting()
        {
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(TurntableSetting));

            try
            {
                StreamWriter sw = new StreamWriter(_turntableSettingFile);
                jsonSerializer.WriteObject(sw.BaseStream, _turntableSetting);
            }
            catch
            {
                MessageBox.Show("速度设置保存失败");
            }
        }

        private void LoadTurntableSetting()
        {
            if (File.Exists(_turntableSettingFile))
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(TurntableSetting));
                try
                {
                    StreamReader streamReader = new StreamReader(_turntableSettingFile);
                    TurntableSetting temp = (TurntableSetting)jsonSerializer.ReadObject(streamReader.BaseStream);
                    _turntableSetting = temp;
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void SaveTextSetting()
        {
            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(TextSetting));

            try
            {
                StreamWriter sw = new StreamWriter(_textSettingFile);
                jsonSerializer.WriteObject(sw.BaseStream, _textSetting);
            }
            catch
            {
                MessageBox.Show("速度设置保存失败");
            }

        }

        private void LoadTextSetting()
        {
            if (File.Exists(_textSettingFile))
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(TextSetting));
                try
                {
                    StreamReader streamReader = new StreamReader(_textSettingFile);
                    TextSetting temp = (TextSetting)jsonSerializer.ReadObject(streamReader.BaseStream);
                    _textSetting = temp;
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void PullRestaurants(object sender, RoutedEventArgs e)
        {
            var result = _networkManager.GetRestaurants();
            if (result.Item1)
            {
                _restaurants.Clear();
                LoadRestaurant();
                InitialRestaurantList();
                MessageBox.Show(result.Item2);
            }
            else
            {
                MessageBox.Show(result.Item2);
            }
        }

        private void PushRestaurants(object sender, RoutedEventArgs e)
        {
            SaveRestaurants();

            var result = _networkManager.UpdateRestaurants(_loadFile);
            if (result.Item1)
            {
                MessageBox.Show(result.Item2);
            }
            else
            {
                MessageBox.Show(result.Item2);
            }
        }

    }
}
