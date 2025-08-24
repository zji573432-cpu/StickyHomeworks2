using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StickyHomeworks.ViewModels;

public class HomeworkEditViewModel : ObservableRecipient
{
    private Paragraph _selectedParagraph = new();
    private Color _textColor = new Color();
    private bool _isBold = false;
    private bool _isItalic = false;
    private bool _isUnderlined = false;
    private bool _isStrikeThrough = false;
    private TextSelection? _selection;
    private bool _isRestoringSelection = false;
    private TextPointer? _beforeTextPointerStart;
    private TextPointer? _beforeTextPointerEnd;
    private ObservableCollection<FontFamily> _fontFamilies = new();
    private FontFamily _font = new();
    private double _fontSize = 14.0;
    private bool _isUpdatingColor = false;

    public Paragraph SelectedParagraph
    {
        get => _selectedParagraph;
        set
        {
            if (Equals(value, _selectedParagraph)) return;
            _selectedParagraph = value;
            OnPropertyChanged();
        }
    }

    public Color TextColor
    {
        get => _textColor;
        set
        {
            if (value.Equals(_textColor)) return;
            _textColor = value;
            OnPropertyChanged();
        }
    }

    public bool IsBold
    {
        get => _isBold;
        set
        {
            if (value == _isBold) return;
            _isBold = value;
            OnPropertyChanged();
        }
    }

    public bool IsItalic
    {
        get => _isItalic;
        set
        {
            if (value == _isItalic) return;
            _isItalic = value;
            OnPropertyChanged();
        }
    }

    public bool IsUnderlined
    {
        get => _isUnderlined;
        set
        {
            if (value == _isUnderlined) return;
            _isUnderlined = value;
            OnPropertyChanged();
        }
    }

    public bool IsStrikeThrough
    {
        get => _isStrikeThrough;
        set
        {
            if (value == _isStrikeThrough) return;
            _isStrikeThrough = value;
            OnPropertyChanged();
        }
    }

    public TextSelection? Selection
    {
        get => _selection;
        set
        {
            if (Equals(value, _selection)) return;
            _selection = value;
            OnPropertyChanged();
        }
    }

    public bool IsRestoringSelection
    {
        get => _isRestoringSelection;
        set
        {
            if (value == _isRestoringSelection) return;
            _isRestoringSelection = value;
            OnPropertyChanged();
        }
    }

    public TextPointer? BeforeTextPointerStart
    {
        get => _beforeTextPointerStart;
        set
        {
            if (Equals(value, _beforeTextPointerStart)) return;
            _beforeTextPointerStart = value;
            OnPropertyChanged();
        }
    }

    public TextPointer? BeforeTextPointerEnd
    {
        get => _beforeTextPointerEnd;
        set
        {
            if (Equals(value, _beforeTextPointerEnd)) return;
            _beforeTextPointerEnd = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<FontFamily> FontFamilies
    {
        get => _fontFamilies;
        set
        {
            if (Equals(value, _fontFamilies)) return;
            _fontFamilies = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<FontWeight> FontWeights { get; } = new ObservableCollection<FontWeight>()
    {
        
    };

    public FontFamily Font
    {
        get => _font;
        set
        {
            if (Equals(value, _font)) return;
            _font = value;
            OnPropertyChanged();
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (value.Equals(_fontSize)) return;
            _fontSize = value;
            OnPropertyChanged();
        }
    }

    public bool IsUpdatingColor
    {
        get => _isUpdatingColor;
        set
        {
            if (value == _isUpdatingColor) return;
            _isUpdatingColor = value;
            OnPropertyChanged();
        }
    }
}