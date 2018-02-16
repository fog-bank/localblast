using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalBlast
{
    public abstract class TabPage : INotifyPropertyChanged
    {
        private string header;
        private PageState state;

        public TabPage(MainViewModel owner)
        {
            Owner = owner;
        }

        public MainViewModel Owner { get; }

        public string Header
        {
            get { return header; }
            set
            {
                header = value;
                OnPropertyChanged();
            }
        }

        public PageState State
        {
            get { return state; }
            set
            {
                state = value;
                OnPropertyChanged();
            }
        }

        public abstract DelegateCommand CloseCommand { get; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //Debug.WriteLine(GetType().Name + "." + propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum PageState
    {
        None,
        New,
        Running,
        Completed,
        Error
    }
}