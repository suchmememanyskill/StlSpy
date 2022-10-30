using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using TextCopy;

namespace StlSpy.Views
{
    public partial class PreviewPostView : UserControlExt<PreviewPostView>
    {
        public PreviewPost Post { get; }
        public ApiDescription Api { get; }
        private bool _downloadedImage = false;
        private ContextMenu _contextMenu = new();

        [Binding(nameof(Panel), "Background")] 
        [Binding(nameof(CheckboxBorder), "Background")]
        public IBrush Color => Api.GetColorAsBrush();

        [Binding(nameof(Title), "Content")] 
        public string PostName => Post.Name;

        public event Action? OnNeedListReload;

        public async void DownloadImage()
        {
            if (_downloadedImage)
                return;

            _downloadedImage = true;
            try
            {
                byte[]? data = await Post.Thumbnail.Get();
                if (data != null)
                {
                    Stream stream = new MemoryStream(data);
                    Background!.Source = Bitmap.DecodeToWidth(stream, 300);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception] {e}");
            }
        }
        
        public PreviewPostView(PreviewPost previewPost, ApiDescription api)
        {
            Post = previewPost;
            Api = api;
            InitializeComponent();
            SetControls();
            UpdateView();
            EffectiveViewportChanged += EffectiveViewportChangedReact;
            
            _contextMenu.ContextMenuOpening += (sender, args) =>
            {
                _contextMenu.Items = new List<Command>() { new("Loading...") }.Select(x => x.ToTemplatedControl())
                    .ToList();
                SetContextMenu();
            };
            TopPanel.ContextMenu = _contextMenu;
            
            CheckboxBorder.IsVisible = !Settings.Get().HidePrintedLabel;

            if (CheckboxBorder.IsVisible)
            {
                CheckBox.IsChecked = Settings.Get().ContainsPrintedUid(Post.UniversalId);
            
                CheckBox.Checked += (_, _) =>
                    Settings.Get().AddPrintedUid(Post.UniversalId);
            
                CheckBox.Unchecked += (_, _) =>
                    Settings.Get().RemovePrintedUid(Post.UniversalId);
            }

            PointerPressed += (sender, args) =>
            {
                if (args.ClickCount == 2)
                    OpenPostDetails();
            };
        }

        public PreviewPostView()
        {
            InitializeComponent();
        }
        
        private void EffectiveViewportChangedReact(object? obj, EffectiveViewportChangedEventArgs args)
        {
            if (args.EffectiveViewport.IsEmpty)
                return;
        
            EffectiveViewportChanged -= EffectiveViewportChangedReact;
            DownloadImage();
        }

        public async Task<List<Command>> GetAddToCollectionList(ICollectionStorage storage)
        {
            List<Command> commands = new()
            {
                new($"Add/Remove to {storage.Name()}")
            };

            List<CollectionId> collections = await storage.GetCollections();
            foreach (var collection in collections)
            {
                if (await storage.IsPostPartOfCollection(Post.UniversalId, collection))
                    commands.Add(new($"Remove from {collection.Name}", () => RemovePostFromCollection(null, storage, collection)));
                else
                    commands.Add(new($"Add to {collection.Name}", () => AddPostToCollection(null, storage, collection)));
            }

            return commands;
        }

        private async void OpenPostDetails()
        {
            PostDetailsWindow window = new(this);
            window.Show(MainWindow.Window!);
        }

        private async void RemovePostFromCollection(Post? post, ICollectionStorage storage, CollectionId collection)
        {
            post ??= await ConvertToPost();
            await storage.RemovePost(collection, post.UniversalId);
            OnNeedListReload?.Invoke();
        }

        private async void SetContextMenu()
        {
            List<Command> commands = new()
            {
                new("Show Details", OpenPostDetails),
                new($"Post: {Post.Name}", () => Utils.Utils.OpenUrl(Post.Website)),
                new($"By: {Post.Author.Name}", () => Utils.Utils.OpenUrl(Post.Author.Website)),
                new("Copy Universal ID to Clipboard", CopyUID),
                new(),
                new("Open in"),
                new("PrusaSlicer", OpenInPrusaSlicer),
                new("Explorer", OpenInExplorer),
                new(),
            };

            commands.AddRange(await GetAddToCollectionList(LocalStorage.Get()));
            commands.Add(new());
            commands.AddRange(await GetAddToCollectionList(OnlineStorage.Get()));
            
            _contextMenu.Items = commands.Select(x => x.ToTemplatedControl()).ToList();
        }

        private async void CopyUID()
        {
            await ClipboardService.SetTextAsync(Post.UniversalId);
        }

        public async Task<Post> ConvertToPost()
        {
            LocalStorage storage = LocalStorage.Get();
            Post? post = await storage.GetPost(Post.UniversalId);
            post ??= (await UnifiedPrintApi.PostsUniversalId(Post.UniversalId))!;
            return post;
        }

        private async Task AddPostToCollection(Post? post, ICollectionStorage storage, CollectionId collection)
        {
            post ??= await ConvertToPost();
            AppTask task = new($"Adding {post.Name} to {collection.Name}");
            await task.WaitUntilReady();
            
            Progress<float> progress = new(x => task.Progress = x);
            
            try
            {
                await storage.AddPost(collection, post, progress);
            }
            catch (Exception e)
            {
                await Utils.Utils.ShowMessageBox("Fail", $"Failed to add post to collection.\n{e.Message}");
            }
            
            task.Complete();
        }
        
        private async Task<string> DownloadPost()
        {
            Post post = await ConvertToPost();
            LocalStorage storage = LocalStorage.Get();
            if (!storage.AreFilesCached(post.UniversalId))
                await AddPostToCollection(post, storage, LocalStorage.DEFAULT_DOWNLOAD_LOCATION);
            
            return (await storage.GetFilesPath(post))!;
        }

        public async void OpenInPrusaSlicer()
        {
            string path = await DownloadPost();
            
            bool result = Utils.Utils.OpenPrusaSlicer(Directory.EnumerateFiles(path)
                .Where(x => new List<string>() { ".stl", ".obj", ".3mf" }.Any(y => x.ToLower().EndsWith(y))).ToList());

            if (!result)
                await Utils.Utils.ShowMessageBox(":(", "Failed to open PrusaSlicer");
        }

        public async void OpenInExplorer()
        {
            string path = await DownloadPost();
            Utils.Utils.OpenFolder(path);
        }
    }
}