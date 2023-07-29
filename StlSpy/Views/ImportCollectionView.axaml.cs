using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;

namespace StlSpy.Views;

public partial class ImportCollectionView : UserControl, IMainView
{
    private Action? _onImport;
    public ImportCollectionView(Action? onImport)
    {
        InitializeComponent();
        _onImport = onImport;
        Submit.Command = new LambdaCommand(x => ImportCollection());
    }
    
    private async void ImportCollection()
    {
        Submit.IsEnabled = false;
        AppTask task = new("Validating Share ID");
        await task.WaitUntilReady();

        OnlineStorage onlineStorage = OnlineStorage.Get();
        GenericCollection? collection = string.IsNullOrEmpty(ShareId.Text)
            ? null
            : await onlineStorage.GetPosts(new CollectionId(ShareId.Text, ""));

        if (collection == null)
        {
            Submit.IsEnabled = true;
            ErrorText.Content = "Shared Collection not found!";
            return;
        }
        
        LocalStorage localStorage = LocalStorage.Get();
        var localCollection = await localStorage.AddCollection(collection.Name.Name);
        
        task.Complete();
        
        await Buttons.AddAllToCollectionNow(collection!, localStorage, localCollection);
        MainWindow.Window?.SetView(new LocalCollectionView(localCollection));
        _onImport?.Invoke();
    }
    
    public string MainText() => "Import Collection";
    public string SubText() => "from code";
    public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
}