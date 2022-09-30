using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views;

public partial class SettingsView : UserControlExt<SettingsView>, IMainView
{
    private Settings _settings = Settings.Get();
    
    public SettingsView()
    {
        InitializeComponent();
        SetControls();
        LocalCollectionPath.Text = _settings.CustomLocalCollectionsPath;
        HidePrintedLabel.IsChecked = _settings.HidePrintedLabel;
    }

    public string MainText() => "Settings";

    public string SubText() => "";

    public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

    [Command(nameof(Save))]
    public async void SaveSettings()
    {
        _settings.CustomLocalCollectionsPath = LocalCollectionPath.Text ?? "";
        _settings.HidePrintedLabel = HidePrintedLabel.IsChecked!.Value;
        _settings.Save();
        await Utils.Utils.ShowMessageBox("Settings", "Saved settings");
    }

    [Command(nameof(LocalCollectionPathBrowse))]
    public async void BrowseLCP()
    {
        OpenFolderDialog dialog = new();
        string? result = await dialog.ShowAsync(MainWindow.Window!);
        if (!string.IsNullOrWhiteSpace(result))
        {
            LocalCollectionPath.Text = result;
        }
    }
}