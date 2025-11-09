using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NZFTC_EmployeeSystem.Views
{
    /// <summary>
    /// Splash screen that displays for 3 seconds before the login window appears
    /// Shows the NZFTC splash image with a loading animation
    /// </summary>
    public partial class SplashScreen : Window
    {
        // Timer to control how long the splash screen displays
        private readonly DispatcherTimer _timer;

        public SplashScreen()
        {
            InitializeComponent();

            // Try to load the splash image from various possible locations
            LoadSplashImage();

            // Create a timer that will close this window after 3 seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // Display for 3 seconds
            };

            // When timer completes, close splash and show login
            _timer.Tick += Timer_Tick;

            // Start the timer when the window loads
            this.Loaded += SplashScreen_Loaded;
        }

        /// <summary>
        /// Attempts to load the splash image from multiple possible locations
        /// Tries various path formats until one succeeds
        /// </summary>
        private void LoadSplashImage()
        {
            try
            {
                // Get the application base directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Calculate the project root directory
                var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName;

                // List of possible image paths to try
                string[] possiblePaths = new string[]
                {
                    // Path 1: Project root Images folder
                    projectRoot != null ? Path.Combine(projectRoot, "Images", "splash.png") : null,
                    
                    // Path 2: Relative to base directory
                    Path.Combine(baseDir, "Images", "splash.png"),
                    
                    // Path 3: One level up from base directory
                    Path.Combine(Directory.GetParent(baseDir)?.FullName ?? baseDir, "Images", "splash.png"),
                    
                    // Path 4: In the base directory directly
                    Path.Combine(baseDir, "splash.png"),
                    
                    // Path 5: NZFTC_EmployeeSystem folder explicitly
                    projectRoot != null ? Path.Combine(projectRoot, "NZFTC_EmployeeSystem", "Images", "splash.png") : null
                };

                // Try each path until we find one that exists
                foreach (var path in possiblePaths)
                {
                    // Skip null paths
                    if (path == null)
                        continue;

                    // Check if the file exists at this path
                    if (File.Exists(path))
                    {
                        try
                        {
                            // Create a new bitmap image
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();

                            // Set the image source to the file path
                            bitmap.UriSource = new Uri(path, UriKind.Absolute);

                            // Cache on load to avoid file locking issues
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();

                            // Set the image source
                            SplashImage.Source = bitmap;

                            // Success - exit the method
                            return;
                        }
                        catch
                        {
                            // If loading this path failed, continue to next path
                            continue;
                        }
                    }
                }

                // If we get here, no paths worked
                // The image will remain empty but the splash screen will still work
            }
            catch
            {
                // If any error occurs, just continue without the image
                // The splash screen will still display and function
            }
        }

        /// <summary>
        /// Start the timer when the splash screen finishes loading
        /// </summary>
        private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        /// <summary>
        /// When timer completes, close splash screen and open login window
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            _timer.Stop();

            // Create and show the login window
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Close the splash screen
            this.Close();
        }
    }
}