using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using TextCopy;

namespace StlSpy.Views
{
    public partial class LocalCollectionView : UserControlExt<LocalCollectionView>, IMainView
    {
        private CollectionId _id;
        private PreviewPostCollectionView _view;
        private string _searchQuery = "";

        public event Action? ReloadTopBar;

        [Binding(nameof(DeleteCollection), "Content")]
        public string DeleteButtonLabel => $"Remove Collection '{_id.Name}'";

        public LocalCollectionView(CollectionId id) : this()
        {
            _id = id;
            SetControls();
            UpdateView();
            _view = new();
            VerticalStackPanel.Children.Add(_view);
            Get();

            DeleteCollection.IsVisible = id.Name != "Downloads" && id.Id != "ALL";

            SearchBox.PropertyChanged += (_, _) =>
            {
                if (_searchQuery == SearchBox.Text || SearchBox.Text == null)
                    return;

                _searchQuery = SearchBox.Text;
                _view.Search(_searchQuery);
            };

            _view.OnNeedListReload += Get;
            
            if (id.Id != "ALL")
                AddTopButtons();
        }

        public LocalCollectionView()
        {
            InitializeComponent();
        }

        private async void AddTopButtons()
        {
            MenuButton addToLocalCollections = await Buttons.AddAllToCollection(() => GetCollection(),
                () => Header.IsEnabled = false, () => Header.IsEnabled = true, LocalStorage.Get(), new() { _id });
            
            Header.Children.Add(addToLocalCollections);
            
            Header.Children.Add(Buttons.CreateButton("Share Collection", OnShareCollection));
            
            Header.Children.Add(Buttons.DumpToJson(GetCollection, () => Header.IsEnabled = false, () => Header.IsEnabled = true));
            Header.Children.Add(Buttons.CreateButton("Export to UIDs", ExportToUids));
        }

        private async void Get()
        {
            await _view.SetPosts(GetPosts());
        }

        private async Task<List<PreviewPostView>> GetPosts()
        {
            LocalStorage storage = LocalStorage.Get();
            if (!storage.ArePostsLoaded())
            {
                AppTask load = new("Waiting until ready...");
                await load.WaitUntilReady();
                load.Complete();
            }
            
            return (await storage.GetPosts(_id))!.Posts.OrderByDescending(x => x.Added).Select(x =>
            {
                var post = new PreviewPostView(x, ApiDescription.GetLocalApiDescription());
                post.OnNeedListReload += Get;
                return post;
            }).ToList();
        }

        private async Task<GenericCollection?> GetCollection()
            => await LocalStorage.Get().GetPosts(_id);

        public string MainText() => "Local";

        public string SubText() => _id.Name;

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(DeleteCollection))]
        public async void Delete()
        {
            var messageBoxStandardWindow = MessageBoxManager
                .GetMessageBoxStandard($"Delete collection {_id.Name}", $"Are you sure you want to delete the collection '{_id.Name}', Including all models that are stored only inside this collection?", ButtonEnum.YesNo);

            if ((await messageBoxStandardWindow.ShowAsync()) != ButtonResult.Yes)
                return;
            
            _view.SetText($"Removing {_id.Name}...");
            Header.IsVisible = false;

            LocalStorage storage = LocalStorage.Get();
            var posts = await storage.GetPosts(_id);

            if (posts == null)
            {
                _view.SetText($"Failed to remove {_id.Name}...");
                return;
            }

            await storage.RemoveCollection(_id);
            
            foreach (var post in posts.Posts)
            {
                if (!await storage.IsPostPartOfCollection(post.UniversalId))
                    await storage.DeleteLocalPost(post.UniversalId);
            }

            _view.SetText($"Removed {_id.Name}");
            ReloadTopBar?.Invoke();
        }

        private async void OnShareCollection()
        {
            Header.IsEnabled = false;
            LocalStorage localStorage = LocalStorage.Get();
            var posts = await localStorage.GetPosts(_id)!;
            
            AppTask task = new("Creating snapshot");
            await task.WaitUntilReady();
            await _view.SetText("Creating snapshot of collection...");
            OnlineStorage storage = OnlineStorage.Get();
            var id = await storage.AddCollection($"{_id.Name} (Shared at {DateTime.Now})", false);

            int successfulCount = 0;
            int nonSuccessfulCount = 0;
            int len = (posts?.Posts?.Count ?? 0);
            
            for (int i = 0; i < len; i++)
            {
                int total = i + 1;
                task.Progress = total / (float)len * 100f;
                task.TextProgress = $"{total}/{len}";
                try
                {
                    await storage.AddPost(id, posts!.Posts![i]);
                    successfulCount++;
                }
                catch
                {
                    nonSuccessfulCount++;
                }
            }

            await ClipboardService.SetTextAsync(id.Id);
            await Utils.Utils.ShowMessageBox("Collection Successfully Shared",
                $"Successfully shared collection. Code is {id.Id} and has been shared to your clipboard\nFailed to share {nonSuccessfulCount} posts. {len} total shared.");

            Header.IsEnabled = true;
            Get();
            task.Complete();
        }

        private async void ExportToUids()
        {
            LocalStorage localStorage = LocalStorage.Get();
            var posts = await localStorage.GetPosts(_id)!;

            List<string> uids = posts!.Posts.Select(x => x.UniversalId).ToList();
            await ClipboardService.SetTextAsync(string.Join(",", uids));
            await Utils.Utils.ShowMessageBox("Export complete", "Copied all UIDs to clipboard.\nYou can paste these in the search field under sites to load them again.");
        }
    }
}