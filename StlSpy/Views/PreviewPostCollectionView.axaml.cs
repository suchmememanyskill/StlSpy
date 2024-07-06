using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using TextCopy;

namespace StlSpy.Views
{
    public partial class PreviewPostCollectionView : UserControl
    {
        private List<PreviewPostView> _posts = new();
        private ContextMenu _contextMenu = new();
        public event Action? OnNeedListReload;
        
        public PreviewPostCollectionView()
        {
            InitializeComponent();
            
            _contextMenu.Opening += (sender, args) =>
            {
                _contextMenu.ItemsSource = new List<Command>() { new("Loading...") }.Select(x => x.ToTemplatedControl())
                    .ToList();
                GenerateCommands();
            };
            List.ContextMenu = _contextMenu;
        }

        public async Task SetText(string text)
        {
            List.ItemsSource = _posts = new();
            Label.Content = text;
            Label.IsVisible = !string.IsNullOrWhiteSpace(text);
            CountLabel.IsVisible = false;
        }

        public async Task SetPosts(List<PreviewPostView> posts)
        {
            if (posts.Count <= 0)
            {
                await SetText("No posts found");
                return;
            }
            
            List.ItemsSource = _posts = posts;
            Label.Content = "";
            Label.IsVisible = false;
            CountLabel.IsVisible = true;
            CountLabel.Content = $"Found {_posts.Count} posts";
        }

        public async Task SetPosts(Task<List<PreviewPostView>> postsTask)
        {
            await SetText("Loading...");
            await SetPosts(await postsTask);
        }

        public void Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _posts.ForEach(x => x.IsVisible = true);
                return;
            }

            query = query.ToLower();
            
            _posts.ForEach(x => x.IsVisible = (x.Post.Author.Name.ToLower().Contains(query) || x.Post.Name.ToLower().Contains(query)));
        }

        private async void GenerateCommands()
        {
            var posts = GetSelectedPosts();

            if (posts.Count <= 0)
            {
                _contextMenu.ItemsSource = new List<Command>() { new Command("Nothing is selected") }
                    .Select(x => x.ToTemplatedControl()).ToList();
                return;
            }

            List<Command> commands = new();

            if (posts.Count == 1)
            {
                commands.Add(new("Show Details", OpenAllDetails));
                commands.Add(new($"Post: {posts[0].Post.Name}", () => Utils.Utils.OpenUrl(posts[0].Post.Website)));
                commands.Add(new($"By: {posts[0].Post.Author.Name}", () => Utils.Utils.OpenUrl(posts[0].Post.Author.Website)));
                commands.Add(new("Copy URL to Clipboard", CopyAllUIDToClipboard));
            }
            else
            {
                commands.Add(new("Multiple items are selected"));
                commands.Add(new("Show Details of All", OpenAllDetails));
                commands.Add(new("Copy URLs to Clipboard", CopyAllUIDToClipboard));
            }

            commands.Add(new());
            commands.Add(new("Open in"));
            commands.Add(new("PrusaSlicer", OpenAllInPrusaSlicer));
            commands.Add(new("Bambu Studio", OpenAllInBambuStudio));
            commands.Add(new("Explorer", OpenAllInExplorer));
            commands.Add(new());

            commands.AddRange(await GenerateAddAndRemoveCommands(LocalStorage.Get()));

            _contextMenu.ItemsSource = commands.Select(x => x.ToTemplatedControl()).ToList();
        }

        private async Task<List<Command>> GenerateAddAndRemoveCommands(ICollectionStorage storage)
        {
            var posts = GetSelectedPosts();
            List<Command> commands = new()
            {
                new($"Add/Remove to {storage.Name()}")
            };

            List<CollectionId> collections = await storage.GetCollections();
            
            foreach (var collection in collections)
            {
                bool anyNotPartOfCollection = false;
                foreach (var previewPostView in posts)
                {
                    if (!(await previewPostView.IsPostPartOfCollection(storage, collection)))
                    {
                        anyNotPartOfCollection = true;
                        break;
                    }
                }
                
                if (!anyNotPartOfCollection)
                    commands.Add(new($"Remove from {collection.Name}", () => RemoveAllFromCollection(storage, collection)));
                else
                    commands.Add(new($"Add to {collection.Name}", () => AddAllToCollection(storage, collection)));
            }

            return commands;
        }

        private async void AddAllToCollection(ICollectionStorage storage, CollectionId collection)
        {
            foreach (var previewPostView in GetSelectedPosts())
            {
                if (!await previewPostView.IsPostPartOfCollection(storage, collection))
                    await previewPostView.AddPostToCollection(null, storage, collection);
            }
        }

        private async void RemoveAllFromCollection(ICollectionStorage storage, CollectionId collection)
        {
            foreach (var previewPostView in GetSelectedPosts())
            {
                if (await previewPostView.IsPostPartOfCollection(storage, collection))
                {
                    await storage.RemovePost(collection, previewPostView.Post.UniversalId);
                }
            }
            
            OnNeedListReload?.Invoke();
        }

        private async void OpenAllInPrusaSlicer()
        {
            List<string> paths = new();
            
            foreach (var previewPostView in GetSelectedPosts())
            {
                string path = await previewPostView.DownloadPost();
                paths.AddRange(Directory.EnumerateFiles(path)
                    .Where(x => new List<string>() { ".stl", ".obj", ".3mf" }.Any(y => x.ToLower().EndsWith(y))).ToList());
            }

            if (!Utils.Utils.OpenPrusaSlicer(paths))
                await Utils.Utils.ShowMessageBox(":(", "Failed to open PrusaSlicer");
        }
        
        private async void OpenAllInBambuStudio()
        {
            List<string> paths = new();
            
            foreach (var previewPostView in GetSelectedPosts())
            {
                string path = await previewPostView.DownloadPost();
                paths.AddRange(Directory.EnumerateFiles(path)
                    .Where(x => new List<string>() { ".stl", ".obj", ".3mf" }.Any(y => x.ToLower().EndsWith(y))).ToList());
            }

            if (!Utils.Utils.OpenBambuStudio(paths))
                await Utils.Utils.ShowMessageBox(":(", "Failed to open Bambu Studio");
        }

        private async void OpenAllInExplorer()
        {
            foreach (var previewPostView in GetSelectedPosts())
            {
                await previewPostView.OpenInExplorer();
            }
        }

        private async void OpenAllDetails()
        {
            foreach (var previewPostView in GetSelectedPosts())
            {
                await previewPostView.OpenPostDetails();
            }
        }

        private async void CopyAllUIDToClipboard()
        {
            await ClipboardService.SetTextAsync(string.Join(", ", GetSelectedPosts().Select(x => x.Post.Website.AbsoluteUri)));
        }

        private List<PreviewPostView> GetSelectedPosts()
        {
            List<PreviewPostView> items = new();
            foreach (var listSelectedItem in List.SelectedItems ?? new List<object>())
            {
                if (listSelectedItem is PreviewPostView view)
                    items.Add(view);
            }

            return items;
        }
    }
}