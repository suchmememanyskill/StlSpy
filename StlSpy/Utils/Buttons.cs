using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Views;
using TextCopy;

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
        AppTask task;
        
        if (storage.AreFilesCached(postView.Post.UniversalId))
        {
            task = new($"Removing {postView.Post.Name}");
            await task.WaitUntilReady();
            
            try
            {
                await storage.DeleteLocalPost(postView.Post.UniversalId);
            }
            catch (Exception e)
            {
                await Utils.ShowMessageBox("Fail", $"Failed to delete post from local storage.\n{e.Message}");
            }
        }

        else
        {
            task = new($"Adding {postView.Post.Name} to Downloads");
            await task.WaitUntilReady();
            
            try
            {
                await storage.AddPost(LocalStorage.DEFAULT_DOWNLOAD_LOCATION, postView.Post);
            }
            catch (Exception e)
            {
                await Utils.ShowMessageBox("Fail", $"Failed to add post to downloads collection.\n{e.Message}");
            }
        }
        
        task.Complete();
        onCompletion?.Invoke(postView);
    }

    public static MenuButton OpenInButton(PostView postView, Action<PostView>? onCompletion = null)
    {
        MenuButton button = new MenuButton("Open in");
        button.Add(new("PrusaSlicer", () => HandleOpenPrusaSlicerButton(postView, onCompletion)));
        button.Add(new("Explorer", () => HandleOpenFolder(postView, onCompletion)));
        button.SetFontSize(14);
        return button;
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
        AppTask task = new($"Adding {postView.Post.Name} to {token.Name}");
        await task.WaitUntilReady();
        
        try
        {
            await storage.AddPost(token, postView.Post);
        }
        catch (Exception e)
        {
            await Utils.ShowMessageBox("Fail", $"Failed to add post to collection.\n{e.Message}");
        }
        
        onCompletion?.Invoke(postView);
        task.Complete();
    }

    public static async Task<MenuButton> AddAllToCollection(Func<Task<GenericCollection?>> getPosts, Action? onStartInvoke,
        Action? onEndInvoke, ICollectionStorage storage, List<CollectionId>? ignore = null)
    {
        ignore ??= new();
        
        var availableCollections = await storage.GetCollections();
        List<Command> commands =
            availableCollections.Where(x => ignore.All(y => y.Id != x.Id)).Select(x => new Command(x.Name,
            () => HandleAddAllToCollection(getPosts, onStartInvoke, onEndInvoke, storage, x))).ToList();

        MenuButton button = new(commands, $"Add all to {storage.Name()}");

        if (commands.Count <= 0)
            button.IsEnabled = false;
        
        button.SetFontSize(14);
        return button;
    }

    private static async void HandleAddAllToCollection(Func<Task<GenericCollection?>> getPosts, Action? onStartInvoke,
        Action? onEndInvoke, ICollectionStorage storage, CollectionId target)
    {
        onStartInvoke?.Invoke();
        AppTask task = new($"Adding multiple posts to {target.Name}");
        await task.WaitUntilReady();

        var posts = await getPosts();

        if (posts == null)
            return;

        int successfulCount = 0;
        int skippedCount = 0;
        int nonSuccessfulCount = 0;
        int totalDone = 0;
        int totalPosts = posts.Posts.Count;

        var collection = await storage.GetPosts(target);

        if (collection == null)
            return;
        
        foreach (var post in posts.Posts)
        {
            totalDone++;
            task.Progress = ((float)totalDone / totalPosts) * 100;
            task.TextProgress = $"{totalDone}/{totalPosts}";
            if (collection.Posts.Any(x => post.UniversalId == x.UniversalId))
            {
                skippedCount++;
                continue;
            }

            try
            {
                await storage.AddPost(target, post);
                successfulCount++;
            }
            catch
            {
                nonSuccessfulCount++;
            }
        }

        await Utils.ShowMessageBox("Transfer done",
            $"Successfully added {successfulCount} posts to {target.Name}. {skippedCount} posts were skipped, {nonSuccessfulCount} posts failed");
        
        task.Complete();
        onEndInvoke?.Invoke();
    }

    public static Button DumpToJson(Func<Task<GenericCollection?>> getPosts, Action? onStartInvoke,
        Action? onEndInvoke)
        => CreateButton("Export to JSON", () => HandleExportToJson(getPosts, onStartInvoke, onEndInvoke));

    private static async void HandleExportToJson(Func<Task<GenericCollection?>> getPosts, Action? onStartInvoke,
        Action? onEndInvoke)
    {
        onStartInvoke?.Invoke();
        var collection = await getPosts();

        if (collection == null)
            return;
        
        string json = JsonConvert.SerializeObject(collection);
        await ClipboardService.SetTextAsync(json);
        await Utils.ShowMessageBox("Clipboard", "Copied JSON to clipboard");
        onEndInvoke?.Invoke();
    }
}