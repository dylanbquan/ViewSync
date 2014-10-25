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

        public void Succeed()
        {

        }
        public void Fail()
        {

        }

        private Nullable<bool> success = null;
        public Nullable<bool> Success
        {
            get { return success; }
            set {
                if (value != success) {
                    success = value;
                    NotifyPropertyChanged("Success");
                }
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
