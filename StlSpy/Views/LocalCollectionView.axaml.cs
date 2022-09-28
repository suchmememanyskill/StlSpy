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

namespace StlSpy.Views
{
    public partial class LocalCollectionView : UserControlExt<LocalCollectionView>, IMainView
    {
        private CollectionId _id;
        private PreviewPostCollectionView _view;
        private PostView? _postView;
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
            _view.OnNewSelection += x =>
            {
                if (x.Post is Post p)
                {
                    _postView = new PostView(p);
                    RespondToButtonRefresh(_postView);
                }
                else
                    _postView = new PostView(x.Post.UniversalId);
                
                _postView.OnInitialised += RespondToButtonRefresh;
                SetControl(_postView);
            };
            VerticalStackPanel.Children.Add(_view);
            Get();

            DeleteCollection.IsVisible = id.Name != "Downloads";

            SearchBox.PropertyChanged += (_, _) =>
            {
                if (_searchQuery == SearchBox.Text || SearchBox.Text == null)
                    return;

                _searchQuery = SearchBox.Text;
                _view.Search(_searchQuery);
            };
            
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
            
            MenuButton addToOnlineCollections = await Buttons.AddAllToCollection(() => GetCollection(),
                () => Header.IsEnabled = false, () => Header.IsEnabled = true, OnlineStorage.Get());
            
            Header.Children.Add(addToOnlineCollections);
            Header.Children.Add(addToLocalCollections);

            if (File.Exists("DEV"))
            {
                Header.Children.Add(Buttons.DumpToJson(GetCollection, () => Header.IsEnabled = false, () => Header.IsEnabled = true));
            }
        }
        
        private async void SetButtonsOnPostView()
        {
            var addToLocalCollection = await Buttons.AddToCollection(_postView!, LocalStorage.Get(), RespondToButtonRefresh);
            var addToOnlineCollection = await Buttons.AddToCollection(_postView!, OnlineStorage.Get(), RespondToButtonRefresh);
            
            _postView?.SetCustomisableButtons(new()
            {
                Buttons.CreateButton($"Remove from {_id.Name}", OnRemove),
                Buttons.OpenInButton(_postView, RespondToButtonRefresh),
                addToOnlineCollection,
                addToLocalCollection
            });
        }

        private async void OnRemove()
        {
            _postView?.SetCustomisableButtonsStatus(false);

            LocalStorage storage = LocalStorage.Get();
            await storage.RemovePost(_id, _postView?.Post.UniversalId ?? "");

            SetControl(null);
            Get();
        }

        private void RespondToButtonRefresh(PostView post)
        {
            if (post == _postView)
                SetButtonsOnPostView();
        }

        private async void Get()
        {
            _view.SetPosts(GetPosts());
        }

        private async Task<List<PreviewPostView>> GetPosts()
        {
            LocalStorage storage = LocalStorage.Get();
            return (await storage.GetPosts(_id))!.Posts.OrderByDescending(x => x.Added).Select(x => new PreviewPostView(x, ApiDescription.GetLocalApiDescription())).ToList();
        }

        private async Task<GenericCollection?> GetCollection()
            => await LocalStorage.Get().GetPosts(_id);
        
        public void SetControl(IControl? control)
        {
            SidePanel.Children.Clear();
            if (control != null)
                SidePanel.Children.Add(control);
        }

        public string MainText() => "Local";

        public string SubText() => _id.Name;

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(DeleteCollection))]
        public async void Delete()
        {
            _view.SetText($"Removing {_id.Name}...");
            SetControl(null);
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
    }
}