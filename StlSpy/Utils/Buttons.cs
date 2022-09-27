using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using StlSpy.Model;
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

    // TODO: Remove?
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
            await storage.AddPost(LocalStorage.DEFAULT_DOWNLOAD_LOCATION, postView.Post);

        onCompletion?.Invoke(postView);
    }

    public static Button OpenPrusaSlicerButton(PostView postView, Action<PostView>? onCompletion = null)
        => CreateButton("Open PrusaSlicer", () => HandleOpenPrusaSlicerButton(postView, onCompletion));

    private static async void HandleOpenPrusaSlicerButton(PostView postView, Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        LocalStorage storage = LocalStorage.Get();
        if (!storage.AreFilesCached(postView.Post.UniversalId))
            await storage.AddPost(LocalStorage.DEFAULT_DOWNLOAD_LOCATION, postView.Post);
        
        string path = (await storage.GetFilesPath(postView.Post))!;

        bool result = Utils.OpenPrusaSlicer(Directory.EnumerateFiles(path)
            .Where(x => new List<string>() { ".stl", ".obj", ".3mf" }.Any(y => x.ToLower().EndsWith(y))).ToList());

        if (!result)
            await Utils.ShowMessageBox(":(", "Failed to open PrusaSlicer");

        onCompletion?.Invoke(postView);
    }
    
    public static Button OpenFolder(PostView postView, Action<PostView>? onCompletion = null)
        => CreateButton("Open Explorer", () => HandleOpenFolder(postView, onCompletion));

    private static async void HandleOpenFolder(PostView postView, Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        LocalStorage storage = LocalStorage.Get();
        if (!storage.AreFilesCached(postView.Post.UniversalId))
            await storage.AddPost(LocalStorage.DEFAULT_DOWNLOAD_LOCATION, postView.Post);
        
        string path = (await storage.GetFilesPath(postView.Post))!;
        
        Utils.OpenFolder(path);
        onCompletion?.Invoke(postView);
    }

    public static async Task<MenuButton> AddToCollection(PostView postView, ICollectionStorage storage, Action<PostView>? onCompletion = null)
    {
        var availableCollections = await storage.GetCollections();
        List<(CollectionId, bool)> convertedCollections = new();

        foreach (var x in availableCollections)
        {
            convertedCollections.Add((x, await storage.IsPostPartOfCollection(postView.Post.UniversalId, x)));
        }

        MenuButton button =
            new(
                convertedCollections.Select(x =>
                    (!x.Item2)
                        ? new Command(x.Item1.Name, () => HandleAddToCollection(postView, x.Item1, storage, onCompletion))
                        : new Command(x.Item1.Name)), $"Add to {storage.Name()}");

        button.SetFontSize(14);
        button.IsEnabled = availableCollections.Count > 0 && convertedCollections.Any(x => !x.Item2);
        return button;
    }
    
    private static async void HandleAddToCollection(PostView postView, CollectionId token, ICollectionStorage storage,
        Action<PostView>? onCompletion = null)
    {
        postView.SetCustomisableButtonsStatus(false);
        await storage.AddPost(token, postView.Post);
        onCompletion?.Invoke(postView);
    }
}