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
            get => header;
            set
            {
                header = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HeaderTooltip));
            }
        }

        public virtual string HeaderTooltip => Header;

        public PageState State
        {
            get => state;
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