using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignColors;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using WindowsShortcutFactory;
using File = System.IO.File;

namespace StickyHomeworks.Models;

public class Settings : ObservableRecipient
{
    private int _selectedPlatteIndex = 0;
    private int _theme = 0;
    private Color _primaryColor = Color.FromRgb(34, 209, 236);
    private Color _secondaryColor  = Color.FromRgb(34, 209, 236);
    private int _colorSource = 1;
    private ObservableCollection<Color> _wallpaperColorPlatte = new();
    private bool _isWallpaperAutoUpdateEnabled = false;
    private int _wallpaperAutoUpdateIntervalSeconds = 60;
    private string _wallpaperClassName = "";
    private bool _isFallbackModeEnabled = true;
    
    private double _targetLightValue = 0.6;
    private double _opacity = 0.7;
    private double _scale = 1.5;
    private ObservableCollection<string> _subjects = new();
    private ObservableCollection<string> _tags = new();
    private bool _isDebugOptionsEnabled = false;
    private double _windowX = 0;
    private double _windowY = 0;
    private double _windowWidth = 400;
    private double _windowHeight = 800;
    private bool _isBottom = true;
    private string _title = "作业";
    private double _maxPanelWidth = 350;
    private bool _isDebugShowInTaskBar = false;
    private bool _autooutwork = true;
    private bool _delayedCleanupEnabled = false;
    private bool _isMainWindowVisible = true;
    private bool _isExpiredMarkEnabled = false;
    private bool _debugginginterface = false;
    private Color _expiredMarkColor = Color.FromRgb(0x33, 0x33, 0x33);
    private ObservableCollection<Color> _savedColors = new();
    private Color _titleColor = Colors.White;
    private FontWeight _titleFontWeight = FontWeights.Normal;
    private int _titleFontWeightValue = 400;
    private bool _isClassIslandIpcEnabled = false;
    private ObservableCollection<SubjectAction> _classIslandSubjects = new();
    private HomeworkTemplateConfig _homeworkTemplate = new();
    private int _updateChannel = 0;
    private bool _isKnotLinkEnabled = true;
    private bool _isKnotLinkHomeworkEnabled = true;
    private bool _isKnotLinkControlEnabled = true;

    public double WindowX
    {
        get => _windowX;
        set
        {
            if (value == _windowX) return;
            _windowX = value;
            OnPropertyChanged();
        }
    }

    public double WindowY
    {
        get => _windowY;
        set
        {
            if (value == _windowY) return;
            _windowY = value;
            OnPropertyChanged();
        }
    }

    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (value == _windowWidth) return;
            _windowWidth = value;
            OnPropertyChanged();
        }
    }

    public double WindowHeight
    {
        get => _windowHeight;
        set
        {
            if (value == _windowHeight) return;
            _windowHeight = value;
            OnPropertyChanged();
        }
    }

    public Color TitleColor
    {
        get => _titleColor;
        set
        {
            if (value.Equals(_titleColor)) return;
            _titleColor = value;
            OnPropertyChanged();
        }
    }
    //<<<字符粗细转换-开始>>>
    [JsonIgnore]
    public FontWeight TitleFontWeight
    {
        get => _titleFontWeight;
        set
        {
            if (value.Equals(_titleFontWeight)) return;
            _titleFontWeight = value;
            if (value == FontWeights.Light) TitleFontWeightValue = 300;
            else if (value == FontWeights.Normal) TitleFontWeightValue = 400;
            else if (value == FontWeights.Medium) TitleFontWeightValue = 500;
            else if (value == FontWeights.Bold) TitleFontWeightValue = 700;
            else TitleFontWeightValue = 400;
            OnPropertyChanged();
        }
    }

    public int TitleFontWeightValue
    {
        get => _titleFontWeightValue;
        set
        {
            if (value == _titleFontWeightValue) return;
            _titleFontWeightValue = value;
            if (value == 300) _titleFontWeight = FontWeights.Light;
            else if (value == 400) _titleFontWeight = FontWeights.Normal;
            else if (value == 500) _titleFontWeight = FontWeights.Medium;
            else if (value == 700) _titleFontWeight = FontWeights.Bold;
            else _titleFontWeight = FontWeights.Normal;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TitleFontWeight));
        }
    }
    //<<<字符粗细转换-结束>>>
    #region General

    public bool IsAutoStartEnabled
    {
        get => File.Exists(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "StickyHomeworks.lnk"));
        set
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "StickyHomeworks.lnk");
            try
            {
                if (value)
                {
                    using var shortcut = new WindowsShortcut();
                    shortcut.Path = Environment.ProcessPath;
                    shortcut.WorkingDirectory = Environment.CurrentDirectory;
                    shortcut.Save(path);
                }
                else
                {
                    File.Delete(path);
                }
                OnPropertyChanged();
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }

    public bool IsBottom
    {
        get => _isBottom;
        set
        {
            if (value == _isBottom) return;
            _isBottom = value;
            OnPropertyChanged();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

        public bool Autooutwork
    {
        get => _autooutwork;
        set
        {
            if (value == _autooutwork) return;
            _autooutwork = value;
            OnPropertyChanged();
        }
    }

    public bool DelayedCleanupEnabled
    {
        get => _delayedCleanupEnabled;
        set
        {
            if (value == _delayedCleanupEnabled) return;
            _delayedCleanupEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsExpiredMarkEnabled
    {
        get => _isExpiredMarkEnabled;
        set
        {
            if (value == _isExpiredMarkEnabled) return;
            _isExpiredMarkEnabled = value;
            OnPropertyChanged();
        }
    }

    public Color ExpiredMarkColor
    {
        get => _expiredMarkColor;
        set
        {
            if (value.Equals(_expiredMarkColor)) return;
            _expiredMarkColor = value;
            OnPropertyChanged();
        }
    }


    public double MaxPanelWidth
    {
        get => _maxPanelWidth;
        set
        {
            if (value.Equals(_maxPanelWidth)) return;
            _maxPanelWidth = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Appearence

    public int Theme
    {
        get => _theme;
        set
        {
            if (value == _theme) return;
            _theme = value;
            OnPropertyChanged();
        }
    }

    public Color PrimaryColor
    {
        get => _primaryColor;
        set
        {
            if (value.Equals(_primaryColor)) return;
            _primaryColor = value;
            OnPropertyChanged();
        }
    }

    public Color SecondaryColor
    {
        get => _secondaryColor;
        set
        {
            if (value.Equals(_secondaryColor)) return;
            _secondaryColor = value;
            OnPropertyChanged();
        }
    }

    public int ColorSource
    {
        get => _colorSource;
        set
        {
            if (value == _colorSource) return;
            _colorSource = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Color> WallpaperColorPlatte
    {
        get => _wallpaperColorPlatte;
        set
        {
            if (Equals(value, _wallpaperColorPlatte)) return;
            _wallpaperColorPlatte = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public Color SelectedPlatte => WallpaperColorPlatte.Count < Math.Max(SelectedPlatteIndex, 0) + 1
        ? Colors.DodgerBlue
        : WallpaperColorPlatte[SelectedPlatteIndex];

    public int SelectedPlatteIndex
    {
        get => _selectedPlatteIndex;
        set
        {
            if (value == _selectedPlatteIndex) return;
            _selectedPlatteIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsWallpaperAutoUpdateEnabled
    {
        get => _isWallpaperAutoUpdateEnabled;
        set
        {
            if (value == _isWallpaperAutoUpdateEnabled) return;
            _isWallpaperAutoUpdateEnabled = value;
            OnPropertyChanged();
        }
    }

    public int WallpaperAutoUpdateIntervalSeconds
    {
        get => _wallpaperAutoUpdateIntervalSeconds;
        set
        {
            if (value == _wallpaperAutoUpdateIntervalSeconds) return;
            _wallpaperAutoUpdateIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public string WallpaperClassName
    {
        get => _wallpaperClassName;
        set
        {
            if (value == _wallpaperClassName) return;
            _wallpaperClassName = value;
            OnPropertyChanged();
        }
    }

    public bool IsFallbackModeEnabled
    {
        get => _isFallbackModeEnabled;
        set
        {
            if (value == _isFallbackModeEnabled) return;
            _isFallbackModeEnabled = value;
            OnPropertyChanged();
        }
    }

    public double TargetLightValue
    {
        get => _targetLightValue;
        set
        {
            if (value.Equals(_targetLightValue)) return;
            _targetLightValue = value;
            OnPropertyChanged();
        }
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            if (value.Equals(_opacity)) return;
            _opacity = value;
            OnPropertyChanged();
        }
    }

    public double Scale
    {
        get => _scale;
        set
        {
            if (value.Equals(_scale)) return;
            _scale = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Subjects

    public ObservableCollection<string> Subjects
    {
        get => _subjects;
        set
        {
            if (Equals(value, _subjects)) return;
            _subjects = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Tags

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


    #endregion
    public bool IsDebugOptionsEnabled
    {
        get => _isDebugOptionsEnabled;
        set
        {
            if (value == _isDebugOptionsEnabled) return;
            _isDebugOptionsEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsDebugShowInTaskBar
    {
        get => _isDebugShowInTaskBar;
        set
        {
            if (value == _isDebugShowInTaskBar) return;
            _isDebugShowInTaskBar = value;
            OnPropertyChanged();
        }
    }

    public bool Debugginginterface
    {
        get => _debugginginterface;
        set
        {
            if (value == _debugginginterface) return;
            _debugginginterface = value;
            OnPropertyChanged();
        }
    }

    public bool IsMainWindowVisible
    {
        get => _isMainWindowVisible;
        set
        {
            if (value == _isMainWindowVisible) return;
            _isMainWindowVisible = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Color> SavedColors
    {
        get => _savedColors;
        set
        {
            if (Equals(value, _savedColors)) return;
            _savedColors = value;
            OnPropertyChanged();
        }
    }

    #region ClassIsland

    public bool IsClassIslandIpcEnabled
    {
        get => _isClassIslandIpcEnabled;
        set
        {
            if (value == _isClassIslandIpcEnabled) return;
            _isClassIslandIpcEnabled = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<SubjectAction> ClassIslandSubjects
    {
        get => _classIslandSubjects;
        set
        {
            if (Equals(value, _classIslandSubjects)) return;
            _classIslandSubjects = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region HomeworkTemplate

    /// <summary>作业模板（快捷操作、通用/科目作业条目）。</summary>
    public HomeworkTemplateConfig HomeworkTemplate
    {
        get => _homeworkTemplate;
        set
        {
            value ??= new HomeworkTemplateConfig();
            if (ReferenceEquals(_homeworkTemplate, value)) return;
            _homeworkTemplate = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Update

    /// <summary>更新渠道：0=稳定版，1=测试版。</summary>
    public int UpdateChannel
    {
        get => _updateChannel;
        set
        {
            if (value == _updateChannel) return;
            _updateChannel = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region KnotLink

    public bool IsKnotLinkEnabled
    {
        get => _isKnotLinkEnabled;
        set
        {
            if (value == _isKnotLinkEnabled) return;
            _isKnotLinkEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsKnotLinkHomeworkEnabled
    {
        get => _isKnotLinkHomeworkEnabled;
        set
        {
            if (value == _isKnotLinkHomeworkEnabled) return;
            _isKnotLinkHomeworkEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool IsKnotLinkControlEnabled
    {
        get => _isKnotLinkControlEnabled;
        set
        {
            if (value == _isKnotLinkControlEnabled) return;
            _isKnotLinkControlEnabled = value;
            OnPropertyChanged();
        }
    }

    #endregion
}

