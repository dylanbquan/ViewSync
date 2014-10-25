using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
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

namespace ViewSyncInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class InstallWindow : Window, INotifyPropertyChanged
    {
        
        public InstallWindow(string title, string initialMessage)
        {
            InitializeComponent();
            Title = title;
            Message = initialMessage;
            //OkayButtonVisibility = Visibility.Collapsed;
            InstallItems = new ObservableCollection<InstallItem>();
            InstallsList.ItemsSource = InstallItems;
        }

        /// <summary>
        /// Install items collection threaded adding
        /// </summary>
        public ObservableCollection<InstallItem> InstallItems;
        public void AddInstallItem(InstallItem child)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                new AddChildDelegate(AddChild), child);
        }
        public delegate void AddChildDelegate(InstallItem child);
        public void AddChild(InstallItem child)
        {
            InstallItems.Add(child);
        }

        /// <summary>
        /// Main message property
        /// </summary>
        private string message;
        public string Message
        {
            get { return message; }
            set {
                if(message != value) {
                    message = value;
                    NotifyPropertyChanged("Message");
                }
            }
        }

        /// <summary>
        /// Complete installation and show final button threaded
        /// </summary>
        public void Complete()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                new CompleteDelegate(CompleteInstall));
        }
        private delegate void CompleteDelegate();
        private void CompleteInstall()
        {
            OkayButton.Visibility = Visibility.Visible;
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// generic property change notification
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
