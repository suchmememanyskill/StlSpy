using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using StlSpy.Service;
using StlSpy.Views;

namespace StlSpy.Utils;

public static class Buttons
{
    public static Button CreateButton(string text, Action action) => new()
    {
        Content = text,
        Command = new LambdaCommand(_ => action())
    };

    public static Button DownloadButton(PostView postView, Action<PostView>? onCompletion = null)
        => CreateButton(LocalStorage.Get().AreFilesCached(postView.Post.UniversalId) ? "Delete" : "Download",
            () => HandleDownloadButton(postView, LocalStorage.Get(), onCompletion));

    private static async void HandleDownloadButton(PostView postView, LocalStorage storage, Action<PostView>? onCompletion)
    {
        // TODO: Give a popup on removal
        postView.SetCustomisableButtonsStatus(false);
        
        if (storage.AreFilesCached(postView.Post.UniversalId))
            await storage.DeleteLocalPost(postView.Post.UniversalId);
        else
            await storage.AddToCollection("Downloads", postView.Post);

        onCompletion?.Invoke(postView);
    }

    public static Button OpenPrusaSlicerButton(PostView postView, Action<PostView>? onCompletion = null)
        => CreateButton("Open PrusaSlicer", () => HandleOpenPrusaSlicerButton(postView, onCompletion));

    private static async void HandleOpenPrusaSlicerButton(PostView postView, Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        LocalStorage storage = LocalStorage.Get();
        if (!storage.AreFilesCached(postView.Post.UniversalId))
            await storage.AddToCollection("Downloads", postView.Post);
        
        string path = (await storage.GetFilesPath(postView.Post))!;

        Utils.OpenPrusaSlicer(Directory.EnumerateFiles(path)
            .Where(x => new List<string>() { ".stl", ".obj", ".3mf" }.Any(y => x.ToLower().EndsWith(y))).ToList());
        
        onCompletion?.Invoke(postView);
    }
    
    public static Button OpenFolder(PostView postView, Action<PostView>? onCompletion = null)
        => CreateButton("Open Explorer", () => HandleOpenFolder(postView, onCompletion));

    private static async void HandleOpenFolder(PostView postView, Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        LocalStorage storage = LocalStorage.Get();
        if (!storage.AreFilesCached(postView.Post.UniversalId))
            await storage.AddToCollection("Downloads", postView.Post);
        
        string path = (await storage.GetFilesPath(postView.Post))!;
        
        Utils.OpenFolder(path);
        onCompletion?.Invoke(postView);
    }

    public static async Task<MenuButton> AddToOnlineCollection(PostView postView, Action<PostView>? onCompletion = null)
    {
        OnlineStorage storage = OnlineStorage.Get();
        Dictionary<string, string> availableCollections = await storage.GetCollections();
        List<(string,string, bool)> convertedCollections = new();

        foreach (var x in availableCollections)
        {
            convertedCollections.Add((x.Key, x.Value, await storage.IsPostPartOfCollection(x.Key, postView.Post.UniversalId)));
        }

        MenuButton button =
            new(
                convertedCollections.Select(x =>
                    (!x.Item3)
                        ? new Command(x.Item2, () => HandleAddToOnlineCollection(postView, x.Item1, onCompletion))
                        : new Command(x.Item1)), "Add to Online Collection");

        button.SetFontSize(14);
        button.IsEnabled = availableCollections.Count > 0 && convertedCollections.Any(x => !x.Item3);
        return button;
    }
    
    private static async void HandleAddToOnlineCollection(PostView postView, string token,
        Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        OnlineStorage storage = OnlineStorage.Get();
        await storage.AddPost(token, postView.Post.UniversalId);
        
        onCompletion?.Invoke(postView);
    }
    
    public static async Task<MenuButton> AddToLocalCollection(PostView postView, Action<PostView>? onCompletion = null)
    {
        LocalStorage storage = LocalStorage.Get();
        List<string> availableCollections = await storage.GetCollectionNames();
        List<(string, bool)> convertedCollections = new();
        bool cached = storage.AreFilesCached(postView.Post.UniversalId);

        foreach (var x in availableCollections)
        {
            convertedCollections.Add((x, await storage.IsPartOfSpecificCollection(x, postView.Post.UniversalId) && cached));
        }

        MenuButton button =
            new(
                convertedCollections.Select(x =>
                    (!x.Item2)
                        ? new Command(x.Item1, () => HandleAddToLocalCollection(postView, x.Item1, onCompletion))
                        : new Command(x.Item1)), "Add to Local Collection");

        button.SetFontSize(14);
        button.IsEnabled = availableCollections.Count > 0 && convertedCollections.Any(x => !x.Item2);
        return button;
    }

    private static async void HandleAddToLocalCollection(PostView postView, string collection,
        Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        LocalStorage storage = LocalStorage.Get();
        await storage.AddToCollection(collection, postView.Post);
        
        onCompletion?.Invoke(postView);
    }
}