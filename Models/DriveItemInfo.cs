using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EverythingModern.Models
{
    public partial class DriveItemInfo : ObservableObject
    {
        [ObservableProperty]
        private string _driveLetter = string.Empty;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private string _volumeLabel = string.Empty;

        [ObservableProperty]
        private string _freeSpaceDisplay = string.Empty;

        [ObservableProperty]
        private string _totalSpaceDisplay = string.Empty;

        [ObservableProperty]
        private int _usedPercent;

        [ObservableProperty]
        private string _fileSystem = string.Empty;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isAllDrives;

        [ObservableProperty]
        private string _iconSymbol = "HardDrive20";

        [ObservableProperty]
        private string _summary = string.Empty;
    }
}
