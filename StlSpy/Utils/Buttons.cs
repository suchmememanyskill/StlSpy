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

    private static async void HandleAddToCollection(Post post, CollectionId token, ICollectionStorage storage,
        Action? onCompletion = null)
    {
        AppTask task = new($"Adding {post.Name} to {token.Name}");
        await task.WaitUntilReady();

        Progress<float> progress = new(x => task.Progress = x);

        try
        {
            await storage.AddPost(token, post, progress);
        }
        catch (Exception e)
        {
            await Utils.ShowMessageBox("Fail", $"Failed to add post to collection.\n{e.Message}");
        }
        
        onCompletion?.Invoke();
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

    public static async Task AddAllToCollectionNow(GenericCollection collection, ICollectionStorage targetStorage, CollectionId targetId)
    {
        await HandleAddAllToCollection(async () => collection, null, null, targetStorage, targetId);
    }
    
    private static async Task HandleAddAllToCollection(Func<Task<GenericCollection?>> getPosts, Action? onStartInvoke,
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

        Progress<float> progress = new(x =>
        {
            string text = task.TextProgress;
            if (text.Contains('('))
                text = text.Split('(').First().Trim();

            task.TextProgress = $"{text} ({x:0.0}%)";
        });
        
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
                await storage.AddPost(target, post, progress);
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