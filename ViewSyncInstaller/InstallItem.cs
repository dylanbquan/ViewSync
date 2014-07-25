using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace ViewSyncInstaller
{
    public class InstallItem : INotifyPropertyChanged
    {

        public InstallItem(string name)
        {
            message = name;
        }

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

        private double level = 0;
        public double Level
        {
            get { return level; }
            set {
                double temp = 0;
                temp = value < 0.0 ? 0.0 : value;
                temp = value > 1.0 ? 1.0 : value;
                if(temp == level) return;

                level = temp;
                NotifyPropertyChanged("Level");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
