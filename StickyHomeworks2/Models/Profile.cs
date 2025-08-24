using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StickyHomeworks.Models;

public class Profile : ObservableRecipient
{
    public ObservableCollection<Homework> Homeworks { get; set; } = new();
}