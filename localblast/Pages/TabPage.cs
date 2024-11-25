using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LocalBlast;

public abstract class TabPage(MainViewModel owner) : INotifyPropertyChanged
{
    private string? header;
    private PageState state;

    public MainViewModel Owner { get; } = owner;

    public string? Header
    {
        get => header;
        set
        {
            header = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HeaderTooltip));
        }
    }

    public virtual string? HeaderTooltip => Header;

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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //Debug.WriteLine(GetType().Name + "." + propertyName);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public enum PageState
{
    None,
    New,
    Running,
    Completed,
    Error
}
