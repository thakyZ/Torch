using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Serialization;
using NLog;
using NLog.Targets.Wrappers;
using Torch.API;

namespace Torch.Server
{
    /// <summary>
    /// Interaction logic for TorchUI.xaml
    /// </summary>
    public partial class TorchUI : Window
    {
        private const string UI_CONFIG = "ui.cfg";

        private readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private TorchServer _server;
        private TorchConfig _config;

        private bool _autoscrollLog = true;

        public TorchUI(TorchServer server)
        {
            if (!TryLoadLocation())
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Width = 800;
                Height = 600;
            }
            
            _config = (TorchConfig)server.Config;
            _server = server;
            //TODO: data binding for whole server
            DataContext = server;
            InitializeComponent();

            AttachConsole();

            Chat.BindServer(server);
            PlayerList.BindServer(server);
            Plugins.BindServer(server);
            LoadConfig((TorchConfig)server.Config);

            Themes.UiSource = this;
            Themes.SetConfig(_config);
            Title = $"{_config.InstanceName} - Torch {server.TorchVersion}, SE {server.GameVersion}";
            
            Loaded += TorchUI_Loaded;
        }

        private void TorchUI_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = FindDescendant<ScrollViewer>(ConsoleText);
            scrollViewer.ScrollChanged += ConsoleText_OnScrollChanged;
        }

        private void AttachConsole()
        {
            const string target = "wpf";
            var doc = LogManager.Configuration.FindTargetByName<FlowDocumentTarget>(target)?.Document;
            if (doc == null)
            {
                var wrapped = LogManager.Configuration.FindTargetByName<WrapperTargetBase>(target);
                doc = (wrapped?.WrappedTarget as FlowDocumentTarget)?.Document;
            }
            ConsoleText.Document = doc ?? new FlowDocument(new Paragraph(new Run("No target!")));
            ConsoleText.TextChanged += ConsoleText_OnTextChanged;
        }

        public static T FindDescendant<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return default(T);
            int numberChildren = VisualTreeHelper.GetChildrenCount(obj);
            if (numberChildren == 0) return default(T);

            for (int i = 0; i < numberChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T)
                {
                    return (T)child;
                }
            }

            for (int i = 0; i < numberChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                var potentialMatch = FindDescendant<T>(child);
                if (potentialMatch != default(T))
                {
                    return potentialMatch;
                }
            }

            return default(T);
        }

        private void ConsoleText_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            if (_autoscrollLog)
                ConsoleText.ScrollToEnd();
        }
        
        private void ConsoleText_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer) sender;
            if (e.ExtentHeightChange == 0)
            {
                // User change.
                _autoscrollLog = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
            }
        }

        public void LoadConfig(TorchConfig config)
        {
            if (!Directory.Exists(config.InstancePath))
                return;

            _config = config;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _server.DedicatedInstance.SaveConfig();
            Task.Run(() => _server.Start());
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to stop the server?", "Stop Server", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
                _server.Invoke(() => _server.Stop());
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Can't save here or you'll persist all the command line arguments
            //
            //var newSize = new Point((int)Width, (int)Height);
            //_config.WindowSize = newSize;
            //var newPos = new Point((int)Left, (int)Top);
            //_config.WindowPosition = newPos;

            //_config.Save(); //you idiot

            var result = MessageBox.Show("Are you sure you want to exit?", "Exit Torch?", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                if (_server?.State == ServerState.Running)
                    _server.Stop();

                SaveLocation();
                
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private bool TryLoadLocation()
        {
            if (!File.Exists(UI_CONFIG))
                return false;

            try
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                using (var s = File.OpenRead(UI_CONFIG))
                {
                    var pos = (WindowPos)new XmlSerializer(typeof(WindowPos)).Deserialize(s);
                    Width = pos.Width;
                    Height = pos.Height;
                    Left = pos.Left;
                    Top = pos.Top;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading window location.");
                return false;
            }

            return true;
        }

        private void SaveLocation()
        {
            var pos = new WindowPos(Left, Top, Width, Height);

            try
            {
                using (var s = File.Create(UI_CONFIG))
                    new XmlSerializer(typeof(WindowPos)).Serialize(s, pos);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving window location.");
            }
        }
        
        /// <summary>
        /// XML serializable class to store window location and size
        /// </summary>
        public class WindowPos
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            
            public WindowPos(double left, double top, double width, double height)
            {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }
            
            public WindowPos() { }
        }
    }
}
