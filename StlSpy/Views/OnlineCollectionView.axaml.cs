using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using TextCopy;

namespace StlSpy.Views
{
    public partial class OnlineCollectionView : UserControlExt<OnlineCollectionView>, IMainView
    {
        private CollectionId _id;
        private PreviewPostCollectionView _view;
        private string _searchQuery = "";

        public event Action? ReloadTopBar;

        [Binding(nameof(CollectionName), "Content")]
        public string CollectionNameStr => _id.Name;

        public OnlineCollectionView(CollectionId id) : this()
        {
            _id = id;
            SetControls();
            UpdateView();
            _view = new();
            VerticalStackPanel.Children.Add(_view);
            Get();
            
            SearchBox.PropertyChanged += (_, _) =>
            {
                if (_searchQuery == SearchBox.Text || SearchBox.Text == null)
                    return;

                _searchQuery = SearchBox.Text;
                _view.Search(_searchQuery);
            };
            
            _view.OnNeedListReload += Get;
            
            AddTopButtons();
        }

        public OnlineCollectionView()
        {
            InitializeComponent();
        }

        private async void AddTopButtons()
        {
            OnlineStorage storage = OnlineStorage.Get();
            Header.Children.Clear();
            Header.Children.Add(CollectionName);
            Header.Children.Add(SearchBox);

            Header.Children.Add((await storage.GetCollections()).Any(x => x.Id == _id.Id)
                ? Buttons.CreateButton("Unpin", UnpinSharedCollection)
                : Buttons.CreateButton("Pin", PinSharedCollection));

            
            MenuButton addToLocalCollections = await Buttons.AddAllToCollection(() => GetCollection(),
                () => Header.IsEnabled = false, () => Header.IsEnabled = true, LocalStorage.Get());
            
            Header.Children.Add(addToLocalCollections);
            
            Header.Children.Add(Buttons.CreateButton("Create new Local Collection", OnImportCollectionViaShareId));
            
            Header.Children.Add(Buttons.DumpToJson(GetCollection, () => Header.IsEnabled = false, () => Header.IsEnabled = true));
            Header.Children.Add(Buttons.CreateButton("Export to UIDs", ExportToUids));
        }

        private async void Get()
        {
            await _view.SetPosts(GetPosts());
        }

        private async Task<List<PreviewPostView>> GetPosts()
        {
            OnlineStorage storage = OnlineStorage.Get();
            return (await storage.GetPosts(_id))!.Posts.Select(x =>
            {
                var post = new PreviewPostView(x, ApiDescription.GetLocalApiDescription());
                post.OnNeedListReload += Get;
                return post;
            }).ToList();
        }
        
        private async Task<GenericCollection?> GetCollection()
            => await OnlineStorage.Get().GetPosts(_id);

        public string MainText() => "Preview Share";
        public string SubText() => "";
        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
        
        private async void OnImportCollectionViaShareId()
        {
            AppTask task = new("Validating Share ID");
            await task.WaitUntilReady();

            OnlineStorage onlineStorage = OnlineStorage.Get();
            GenericCollection? collection = string.IsNullOrEmpty(_id.Id)
                ? null
                : await onlineStorage.GetPosts(_id);

            if (collection == null)
                return;
            
            LocalStorage localStorage = LocalStorage.Get();
            var localCollection = await localStorage.AddCollection(collection.Name.Name);
        
            task.Complete();
        
            await Buttons.AddAllToCollectionNow(collection!, localStorage, localCollection);
            MainWindow.Window?.SetView(new LocalCollectionView(localCollection));
            ReloadTopBar?.Invoke();
        }
        
        private async void ExportToUids()
        {
            LocalStorage localStorage = LocalStorage.Get();
            var posts = await localStorage.GetPosts(_id)!;

            List<string> uids = posts!.Posts.Select(x => x.UniversalId).ToList();
            await ClipboardService.SetTextAsync(string.Join(",", uids));
            await Utils.Utils.ShowMessageBox("Export complete", "Copied all UIDs to clipboard.\nYou can paste these in the search field under sites to load them again.");
        }

        private async void PinSharedCollection()
        {
            OnlineStorage storage = OnlineStorage.Get();
            await storage.SaveCollectionToDisk(_id.Id, _id.Name);
            ReloadTopBar?.Invoke();
            AddTopButtons();
        }

        private async void UnpinSharedCollection()
        {
            OnlineStorage storage = OnlineStorage.Get();
            await storage.RemoveCollection(_id);
            ReloadTopBar?.Invoke();
            AddTopButtons();
        }
    }
}