using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StickyHomeworks.Models;

public class Homework : ObservableRecipient
{
    private Guid _id = Guid.NewGuid();
    private string _content = "";
    private string _subject = "";
    private DateTime _dueTime = DateTime.Today;
    private ObservableCollection<string> _tags = new();
    private DateTime? _firstExpiredShowTime;

    public Guid Id
    {
        get => _id;
        set
        {
            if (value == _id) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (value == _content) return;
            _content = value;
            OnPropertyChanged();
        }
    }

    public string Subject
    {
        get => _subject;
        set
        {
            if (value == _subject) return;
            _subject = value;
            OnPropertyChanged();
        }
    }

    public DateTime DueTime
    {
        get => _dueTime;
        set
        {
            if (value.Equals(_dueTime)) return;
            _dueTime = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Tags
    {
        get => _tags;
        set
        {
            if (Equals(value, _tags)) return;
            _tags = value;
            OnPropertyChanged();
        }
    }

    public DateTime? FirstExpiredShowTime
    {
        get => _firstExpiredShowTime;
        set
        {
            if (value.Equals(_firstExpiredShowTime)) return;
            _firstExpiredShowTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsExpired => DueTime.Date < DateTime.Today;
}