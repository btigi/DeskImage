﻿using DeskImage;
using System.ComponentModel;
using System.Diagnostics;
using Forms = System.Windows;

public class Instance : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private double _posX;
    private double _posY;
    private double _width;
    private double _height;
    private string _name;
    private string _folder;
    private string _titleFontFamily;
    private bool _minimized;
    private bool _isLocked;
    private string _titleBarColor = "#0C000000";
    private string _titleTextColor = "#FFFFFF";
    private string _borderColor = "#FFFFFF";
    private bool _borderEnabled = false;
    private Forms.HorizontalAlignment _titleTextAlignment = Forms.HorizontalAlignment.Center;
    private string? _titleText;
    private string _listViewBackgroundColor = "#0C000000";
    private int _opacity = 26;
    private double _titleFontSize = 12;

    public double PosX
    {
        get => _posX;
        set
        {
            if (_posX != value)
            {
                _posX = value;
                OnPropertyChanged(nameof(PosX), value.ToString());
            }
        }
    }

    public double PosY
    {
        get => _posY;
        set
        {
            if (_posY != value)
            {
                _posY = value;
                OnPropertyChanged(nameof(PosY), value.ToString());
            }
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged(nameof(Width), value.ToString());
            }
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged(nameof(Height), value.ToString());
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name), value);
            }
        }
    }
    public string Folder
    {
        get => _folder;
        set
        {
            if (_folder != value)
            {
                _folder = value;
                OnPropertyChanged(nameof(Folder), value);
            }
        }
    }

    public string TitleFontFamily
    {
        get => _titleFontFamily;
        set
        {
            if (_titleFontFamily != value)
            {
                _titleFontFamily = value;
                OnPropertyChanged(nameof(TitleFontFamily), value);
            }
        }
    }

    public bool Minimized
    {
        get => _minimized;
        set
        {
            if (_minimized != value)
            {
                _minimized = value;
                OnPropertyChanged(nameof(Minimized), value.ToString());
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged(nameof(IsLocked), value.ToString());
            }
        }
    }

    public string TitleBarColor
    {
        get => _titleBarColor;
        set
        {
            if (_titleBarColor != value)
            {
                _titleBarColor = value;
                OnPropertyChanged(nameof(TitleBarColor), value);
            }
        }
    }


    public string TitleTextColor
    {
        get => _titleTextColor;
        set
        {
            if (_titleTextColor != value)
            {
                _titleTextColor = value;
                OnPropertyChanged(nameof(TitleTextColor), value);
            }
        }
    }

    public string BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor != value)
            {
                _borderColor = value;
                OnPropertyChanged(nameof(BorderColor), value);
            }
        }
    }

    public bool BorderEnabled
    {
        get => _borderEnabled;
        set
        {
            if (_borderEnabled != value)
            {
                _borderEnabled = value;
                OnPropertyChanged(nameof(BorderEnabled), value.ToString());
            }
        }
    }

    public string? TitleText
    {
        get => _titleText;
        set
        {
            if (_titleText != value)
            {
                _titleText = value;
                OnPropertyChanged(nameof(TitleText), value);
            }
        }
    }

    public Forms.HorizontalAlignment TitleTextAlignment
    {
        get => _titleTextAlignment;
        set
        {
            if (_titleTextAlignment != value)
            {
                _titleTextAlignment = value;
                OnPropertyChanged(nameof(TitleTextAlignment), value.ToString());
            }
        }
    }

    public string ListViewBackgroundColor
    {
        get => _listViewBackgroundColor;
        set
        {
            if (_listViewBackgroundColor != value)
            {
                _listViewBackgroundColor = value;
                OnPropertyChanged(nameof(ListViewBackgroundColor), value);
            }
        }
    }

    public int Opacity
    {
        get => _opacity;
        set
        {
            if (_opacity != value)
            {
                _opacity = value;
                OnPropertyChanged(nameof(Opacity), value.ToString());
            }
        }
    }
    
    public double TitleFontSize
    {
        get => _titleFontSize;
        set
        {
            if (_titleFontSize != value)
            {
                _titleFontSize = value;
                OnPropertyChanged(nameof(TitleFontSize), value.ToString());
            }
        }
    }

    public Instance(Instance instance)
    {
        _posX = instance._posX;
        _posY = instance._posY;
        _width = instance.Width;
        _height = instance._height;
        _name = instance._name;
        _minimized = instance._minimized;
        _folder = instance._folder;
        _isLocked = instance._isLocked;
        _titleBarColor = instance._titleBarColor;
        _titleTextColor = instance._titleTextColor;
        _borderColor = instance._borderColor;
        _borderEnabled = instance._borderEnabled;
        _titleTextAlignment = instance._titleTextAlignment;
        _listViewBackgroundColor = instance._listViewBackgroundColor;
        _opacity = instance._opacity;
        _titleFontSize = instance._titleFontSize;
    }

    public Instance(string name) // default instance
    {
        _width = 175;
        _height = 215;
        _posX = Screen.PrimaryScreen!.Bounds.Width / 2 - _width / 2;
        _posY = Screen.PrimaryScreen!.Bounds.Height / 2 - _height / 2;
        _name = name;
        _minimized = false;
        _folder = "empty";
        _isLocked = false;

    }

    protected void OnPropertyChanged(string propertyName, string value)
    {

        if (propertyName == "Name")
        {
            Debug.WriteLine($"oldname: {_name} \t newname: {Name}");
            if (Name == "empty")
            {
                MainWindow._controller.WriteOverInstanceToKey(this, "empty");

            }
        }
        else
        {
            //  Debug.WriteLine($"Property {propertyName} has changed.");

            if (Name != "empty")
            {
                MainWindow._controller.reg.WriteToRegistry(propertyName, value, this);

            }

        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string GetKeyLocation()
    {
        if (_name != null && Name != null)
        {
            return @$"SOFTWARE\DeskImage\Instances\{Name}";

        }
        return "";
    }
}