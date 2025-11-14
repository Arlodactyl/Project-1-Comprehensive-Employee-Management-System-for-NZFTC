using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using NZFTC_EmployeeSystem.Data;
using NZFTC_EmployeeSystem.Models;
using Path = System.Windows.Shapes.Path;
using Ellipse = System.Windows.Shapes.Ellipse;
using Line = System.Windows.Shapes.Line;

namespace NZFTC_EmployeeSystem.Views
{
    public partial class DashboardHomePage : Page
    {
        private readonly AppDbContext _dbContext;
        private readonly User _currentUser;

        public DashboardHomePage(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _dbContext = new AppDbContext();

            LoadBuildingImage();
            LoadSummary();
            CustomizeQuickLinks();
            LoadWeatherData();

            this.Unloaded += DashboardHomePage_Unloaded;
        }

        // Below customizes quick links based on user role
        private void CustomizeQuickLinks()
        {
            if (_currentUser.Role == "Admin")
            {
                LeaveManagementButton.Content = "Leave Management";
                LeaveManagementButton.Click += LeaveManagement_Click;

                PayrollButton.Content = "Payroll";
                PayrollButton.Click += Payroll_Click;

                DepartmentsButton.Content = "Departments";
                DepartmentsButton.Click += Departments_Click;
            }
            else if (_currentUser.Role == "Employee")
            {
                LeaveManagementButton.Content = "My Leave";
                LeaveManagementButton.Click += MyLeave_Click;

                PayrollButton.Content = "My Pay";
                PayrollButton.Click += MyPay_Click;

                DepartmentsButton.Content = "My Training";
                DepartmentsButton.Click += MyTraining_Click;
            }
            else if (_currentUser.Role == "Workplace Trainer")
            {
                LeaveManagementButton.Content = "Employee Management";
                LeaveManagementButton.Click += EmployeeManagement_Click;

                PayrollButton.Content = "Training Records";
                PayrollButton.Click += TrainingRecords_Click;

                DepartmentsButton.Content = "Departments";
                DepartmentsButton.Click += Departments_Click;
            }
        }

        // Navigation methods
        private void LeaveManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        private void Payroll_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToPayroll();
            }
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToDepartments();
            }
        }

        private void MyLeave_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToLeaveManagement();
            }
        }

        private void MyPay_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyPay();
            }
        }

        private void MyTraining_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToMyTraining();
            }
        }

        private void EmployeeManagement_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        private void TrainingRecords_Click(object sender, RoutedEventArgs e)
        {
            var dashboardWindow = Window.GetWindow(this) as DashboardWindow;
            if (dashboardWindow != null)
            {
                dashboardWindow.NavigateToEmployeeManagement();
            }
        }

        // Below tries multiple paths to load building image
        private void LoadBuildingImage()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;

                string[] possiblePaths = new string[]
                {
                    projectRoot != null ? System.IO.Path.Combine(projectRoot, "Images", "building.png") : null,
                    System.IO.Path.Combine(baseDir, "Images", "building.png"),
                    System.IO.Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "Images", "building.png"),
                    System.IO.Path.Combine(baseDir, "building.png"),
                    projectRoot != null ? System.IO.Path.Combine(projectRoot, "NZFTC_EmployeeSystem", "Images", "building.png") : null
                };

                foreach (var path in possiblePaths)
                {
                    if (path == null)
                        continue;

                    if (File.Exists(path))
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            BuildingImage.Source = bitmap;
                            ImagePlaceholder.Visibility = Visibility.Collapsed;
                            return;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                ShowImagePlaceholder();
            }
            catch (Exception ex)
            {
                ShowImagePlaceholder();
                System.Diagnostics.Debug.WriteLine($"Error loading building image: {ex.Message}");
            }
        }

        private void ShowImagePlaceholder()
        {
            ImagePlaceholder.Visibility = Visibility.Visible;
            BuildingImage.Source = null;
        }

        // Below loads statistics from database
        private void LoadSummary()
        {
            TotalEmployeesText.Text = _dbContext.Employees.Count().ToString();
            PendingLeaveText.Text = _dbContext.LeaveRequests.Count(l => l.Status == "Pending").ToString();
            OpenGrievanceText.Text = _dbContext.Grievances.Count(g => g.Status == "Open").ToString();
        }

        // Below loads live weather data from Open-Meteo API (free, no API key required!)
        private async void LoadWeatherData()
        {
            try
            {
                // Open-Meteo API - Completely free, open-source, no API key needed!
                // Christchurch, NZ coordinates
                double latitude = -43.5321;
                double longitude = 172.6362;
                string apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&timezone=Pacific/Auckland";

                using (HttpClient client = new HttpClient())
                {
                    // Set timeout to 5 seconds
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var response = await client.GetStringAsync(apiUrl);
                    var weatherData = JObject.Parse(response);

                    // Extract current weather information
                    var current = weatherData["current"];
                    double temperature = current["temperature_2m"].Value<double>();
                    int weatherCode = current["weather_code"].Value<int>();
                    double windSpeed = current["wind_speed_10m"].Value<double>();
                    double windDirection = current["wind_direction_10m"].Value<double>();

                    // Get weather description from WMO code
                    string description = GetWeatherDescription(weatherCode);
                    string windDir = GetWindDirection(windDirection);

                    // Update UI on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                        WeatherTemperatureText.Text = Math.Round(temperature).ToString();
                        WeatherDescriptionText.Text = $"{description} - {windDir} {Math.Round(windSpeed)} km/h";

                        // Display animated weather icon
                        DisplayWeatherIcon(weatherCode);
                    });
                }
            }
            catch (HttpRequestException)
            {
                // Network error - show fallback data
                ShowFallbackWeather("Unable to connect to weather service");
            }
            catch (TaskCanceledException)
            {
                // Timeout - show fallback data
                ShowFallbackWeather("Weather service timeout");
            }
            catch (Exception ex)
            {
                // Any other error - show fallback data
                ShowFallbackWeather($"Weather unavailable");
                System.Diagnostics.Debug.WriteLine($"Weather API Error: {ex.Message}");
            }
        }

        // Below converts WMO weather code to description
        private string GetWeatherDescription(int code)
        {
            switch (code)
            {
                case 0: return "Clear Sky";
                case 1: return "Mainly Clear";
                case 2: return "Partly Cloudy";
                case 3: return "Overcast";
                case 45:
                case 48: return "Foggy";
                case 51:
                case 53:
                case 55: return "Light Drizzle";
                case 56:
                case 57: return "Freezing Drizzle";
                case 61: return "Light Rain";
                case 63: return "Moderate Rain";
                case 65: return "Heavy Rain";
                case 66:
                case 67: return "Freezing Rain";
                case 71: return "Light Snow";
                case 73: return "Moderate Snow";
                case 75: return "Heavy Snow";
                case 77: return "Snow Grains";
                case 80:
                case 81:
                case 82: return "Rain Showers";
                case 85:
                case 86: return "Snow Showers";
                case 95: return "Thunderstorm";
                case 96:
                case 99: return "Thunderstorm with Hail";
                default: return "Unknown";
            }
        }

        // Below converts wind degree to compass direction
        private string GetWindDirection(double degrees)
        {
            string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = (int)Math.Round(degrees / 45.0) % 8;
            return directions[index];
        }

        // Below shows fallback weather data when API fails
        private void ShowFallbackWeather(string reason)
        {
            Dispatcher.Invoke(() =>
            {
                WeatherTemperatureText.Text = "18";
                WeatherDescriptionText.Text = "Clear Skies - NE 15 km/h";
                DisplayWeatherIcon(0); // Clear sky icon
                System.Diagnostics.Debug.WriteLine($"Using fallback weather: {reason}");
            });
        }

        // Below closes database connection when page unloads
        private void DashboardHomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            _dbContext?.Dispose();
        }

        // Below displays animated weather icon based on weather code
        private void DisplayWeatherIcon(int weatherCode)
        {
            WeatherIconCanvas.Children.Clear();

            // Determine which icon to display based on weather code
            if (weatherCode == 0) // Clear sky
            {
                CreateSunIcon();
            }
            else if (weatherCode >= 1 && weatherCode <= 3) // Cloudy
            {
                CreateCloudIcon(weatherCode);
            }
            else if (weatherCode >= 45 && weatherCode <= 48) // Fog
            {
                CreateFogIcon();
            }
            else if ((weatherCode >= 51 && weatherCode <= 67) || (weatherCode >= 80 && weatherCode <= 82)) // Rain
            {
                CreateRainIcon();
            }
            else if ((weatherCode >= 71 && weatherCode <= 77) || (weatherCode >= 85 && weatherCode <= 86)) // Snow
            {
                CreateSnowIcon();
            }
            else if (weatherCode >= 95 && weatherCode <= 99) // Thunderstorm
            {
                CreateThunderstormIcon();
            }
            else // Default to partly cloudy
            {
                CreateCloudIcon(2);
            }
        }

        // Below creates animated sun icon
        private void CreateSunIcon()
        {
            // Sun circle
            Ellipse sun = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(Color.FromRgb(255, 204, 0)),
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(sun, 30);
            Canvas.SetTop(sun, 30);

            // Animate sun pulsing
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            ScaleTransform scaleTransform = new ScaleTransform(1, 1);
            sun.RenderTransform = scaleTransform;
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

            // Sun rays
            for (int i = 0; i < 8; i++)
            {
                double angle = i * 45;
                double radian = angle * Math.PI / 180;

                Line ray = new Line
                {
                    X1 = 50 + Math.Cos(radian) * 25,
                    Y1 = 50 + Math.Sin(radian) * 25,
                    X2 = 50 + Math.Cos(radian) * 35,
                    Y2 = 50 + Math.Sin(radian) * 35,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 204, 0)),
                    StrokeThickness = 3,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                WeatherIconCanvas.Children.Add(ray);
            }

            WeatherIconCanvas.Children.Add(sun);
        }

        // Below creates cloud icon with different densities
        private void CreateCloudIcon(int density)
        {
            // Main cloud body
            Ellipse cloud1 = new Ellipse
            {
                Width = 35,
                Height = 25,
                Fill = new SolidColorBrush(density == 1 ? Color.FromRgb(189, 195, 199) : Color.FromRgb(149, 165, 166))
            };
            Canvas.SetLeft(cloud1, 25);
            Canvas.SetTop(cloud1, 40);

            Ellipse cloud2 = new Ellipse
            {
                Width = 25,
                Height = 20,
                Fill = new SolidColorBrush(density == 1 ? Color.FromRgb(189, 195, 199) : Color.FromRgb(149, 165, 166))
            };
            Canvas.SetLeft(cloud2, 45);
            Canvas.SetTop(cloud2, 35);

            Ellipse cloud3 = new Ellipse
            {
                Width = 30,
                Height = 23,
                Fill = new SolidColorBrush(density == 1 ? Color.FromRgb(189, 195, 199) : Color.FromRgb(149, 165, 166))
            };
            Canvas.SetLeft(cloud3, 35);
            Canvas.SetTop(cloud3, 42);

            // If partly cloudy (density 1 or 2), add sun peeking out
            if (density <= 2)
            {
                Ellipse sun = new Ellipse
                {
                    Width = 25,
                    Height = 25,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 204, 0))
                };
                Canvas.SetLeft(sun, 15);
                Canvas.SetTop(sun, 25);
                WeatherIconCanvas.Children.Add(sun);
            }

            WeatherIconCanvas.Children.Add(cloud1);
            WeatherIconCanvas.Children.Add(cloud2);
            WeatherIconCanvas.Children.Add(cloud3);

            // Animate cloud floating
            TranslateTransform cloudTransform = new TranslateTransform();
            cloud1.RenderTransform = cloudTransform;
            cloud2.RenderTransform = cloudTransform;
            cloud3.RenderTransform = cloudTransform;

            DoubleAnimation floatAnimation = new DoubleAnimation
            {
                From = 0,
                To = 5,
                Duration = TimeSpan.FromSeconds(3),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            cloudTransform.BeginAnimation(TranslateTransform.XProperty, floatAnimation);
        }

        // Below creates fog icon
        private void CreateFogIcon()
        {
            for (int i = 0; i < 4; i++)
            {
                Line fogLine = new Line
                {
                    X1 = 20,
                    Y1 = 30 + i * 12,
                    X2 = 80,
                    Y2 = 30 + i * 12,
                    Stroke = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                    StrokeThickness = 4,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Opacity = 0.7
                };
                WeatherIconCanvas.Children.Add(fogLine);

                // Animate fog movement
                DoubleAnimation fogAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 10,
                    Duration = TimeSpan.FromSeconds(2 + i * 0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                TranslateTransform transform = new TranslateTransform();
                fogLine.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, fogAnimation);
            }
        }

        // Below creates animated rain icon
        private void CreateRainIcon()
        {
            // Cloud
            CreateCloudIcon(3);

            // Rain drops
            for (int i = 0; i < 5; i++)
            {
                Line raindrop = new Line
                {
                    X1 = 30 + i * 10,
                    Y1 = 65,
                    X2 = 28 + i * 10,
                    Y2 = 85,
                    Stroke = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    StrokeThickness = 2,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                WeatherIconCanvas.Children.Add(raindrop);

                // Animate rain falling
                DoubleAnimation rainAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 20,
                    Duration = TimeSpan.FromSeconds(0.8),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                rainAnimation.BeginTime = TimeSpan.FromSeconds(i * 0.15);

                TranslateTransform transform = new TranslateTransform();
                raindrop.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.YProperty, rainAnimation);

                // Fade out as it falls
                DoubleAnimation fadeAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.8),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                fadeAnimation.BeginTime = TimeSpan.FromSeconds(i * 0.15);
                raindrop.BeginAnimation(Line.OpacityProperty, fadeAnimation);
            }
        }

        // Below creates snow icon
        private void CreateSnowIcon()
        {
            // Cloud
            CreateCloudIcon(3);

            // Snowflakes
            for (int i = 0; i < 5; i++)
            {
                Ellipse snowflake = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(snowflake, 30 + i * 10);
                Canvas.SetTop(snowflake, 65);
                WeatherIconCanvas.Children.Add(snowflake);

                // Animate snow falling slowly
                DoubleAnimation snowAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 25,
                    Duration = TimeSpan.FromSeconds(2),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                snowAnimation.BeginTime = TimeSpan.FromSeconds(i * 0.3);

                // Add slight horizontal drift
                DoubleAnimation driftAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 5,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                TranslateTransform transform = new TranslateTransform();
                snowflake.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.YProperty, snowAnimation);
                transform.BeginAnimation(TranslateTransform.XProperty, driftAnimation);
            }
        }

        // Below creates thunderstorm icon
        private void CreateThunderstormIcon()
        {
            // Dark cloud
            CreateCloudIcon(3);

            // Lightning bolt
            PathGeometry lightningGeometry = new PathGeometry();
            PathFigure figure = new PathFigure { StartPoint = new Point(50, 60) };
            figure.Segments.Add(new LineSegment(new Point(45, 75), true));
            figure.Segments.Add(new LineSegment(new Point(52, 75), true));
            figure.Segments.Add(new LineSegment(new Point(47, 90), true));
            figure.Segments.Add(new LineSegment(new Point(55, 70), true));
            figure.Segments.Add(new LineSegment(new Point(48, 70), true));
            figure.Segments.Add(new LineSegment(new Point(50, 60), true));
            lightningGeometry.Figures.Add(figure);

            Path lightning = new Path
            {
                Data = lightningGeometry,
                Fill = new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                Stroke = new SolidColorBrush(Color.FromRgb(243, 156, 18)),
                StrokeThickness = 1
            };
            WeatherIconCanvas.Children.Add(lightning);

            // Animate lightning flash
            DoubleAnimation flashAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = TimeSpan.FromSeconds(0.15),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(10))
            };
            flashAnimation.BeginTime = TimeSpan.FromSeconds(1);
            lightning.BeginAnimation(Path.OpacityProperty, flashAnimation);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpMessage = "Dashboard Overview Help\n\n" +
                "This dashboard provides a quick overview of company statistics and shortcuts to common tasks.\n\n" +
                "Statistics Cards:\n" +
                "- Total Employees: Shows the current number of active employees\n" +
                "- Pending Leave Requests: Leave requests awaiting approval\n" +
                "- Open Grievances: Unresolved employee grievances\n\n" +
                "Quick Links:\n" +
                "- Use the buttons on the right to quickly navigate to frequently used pages\n" +
                "- Button labels change based on your role (Admin, Employee, or Trainer)\n\n" +
                "Company News:\n" +
                "- View important company announcements and updates";

            MessageBox.Show(
                helpMessage,
                "Dashboard Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}