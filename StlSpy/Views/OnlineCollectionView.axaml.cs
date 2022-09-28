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
        private PostView? _postView;
        private string _searchQuery = "";

        [Binding(nameof(DeleteCollection), "Content")]
        public string DeleteButtonLabel => $"Remove Collection '{_id.Name}'";
        
        [Binding(nameof(ShareCollection), "Content")]
        public string ShareButtonLabel => $"Share Collection '{_id.Name}'";
        
        public event Action? ReloadTopBar;

        public OnlineCollectionView(CollectionId id) : this()
        {
            _id = id;
            SetControls();
            UpdateView();
            _view = new();
            _view.OnNewSelection += x =>
            {
                _postView = new PostView(x.Post.UniversalId);
                
                _postView.OnInitialised += RespondToButtonRefresh;
                SetControl(_postView);
            };
            VerticalStackPanel.Children.Add(_view);
            Get();
            
            SearchBox.PropertyChanged += (_, _) =>
            {
                if (_searchQuery == SearchBox.Text || SearchBox.Text == null)
                    return;

                _searchQuery = SearchBox.Text;
                _view.Search(_searchQuery);
            };
            
            AddTopButtons();
        }

        public OnlineCollectionView()
        {
            InitializeComponent();
        }
        
        private async void SetButtonsOnPostView()
        {
            var addToLocalCollection = await Buttons.AddToCollection(_postView!, LocalStorage.Get(), RespondToButtonRefresh);
            var addToOnlineCollection = await Buttons.AddToCollection(_postView!, OnlineStorage.Get(), RespondToButtonRefresh);
            
            _postView?.SetCustomisableButtons(new()
            {
                Buttons.CreateButton($"Remove from {_id.Name}", OnRemove),
                Buttons.OpenPrusaSlicerButton(_postView, RespondToButtonRefresh),
                Buttons.OpenFolder(_postView, RespondToButtonRefresh),
                addToOnlineCollection,
                addToLocalCollection
            });
        }
        
        private async void AddTopButtons()
        {
            MenuButton addToLocalCollections = await Buttons.AddAllToCollection(() => GetCollection(),
                () => Header.IsEnabled = false, () => Header.IsEnabled = true, LocalStorage.Get());
            
            MenuButton addToOnlineCollections = await Buttons.AddAllToCollection(() => GetCollection(),
                () => Header.IsEnabled = false, () => Header.IsEnabled = true, OnlineStorage.Get(), new() { _id });
            
            Header.Children.Add(addToOnlineCollections);
            Header.Children.Add(addToLocalCollections);
            
            if (File.Exists("DEV"))
            {
                Header.Children.Add(Buttons.DumpToJson(GetCollection, () => Header.IsEnabled = false, () => Header.IsEnabled = true));
            }
        }

        private async void OnRemove()
        {
            _postView?.SetCustomisableButtonsStatus(false);

            OnlineStorage storage = OnlineStorage.Get();
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
            OnlineStorage storage = OnlineStorage.Get();
            return (await storage.GetPosts(_id))!.Posts.Select(x => new PreviewPostView(x, ApiDescription.GetLocalApiDescription())).ToList();
        }
        
        private async Task<GenericCollection?> GetCollection()
            => await OnlineStorage.Get().GetPosts(_id);
        
        public void SetControl(IControl? control)
        {
            SidePanel.Children.Clear();
            if (control != null)
                SidePanel.Children.Add(control);
        }

        public string MainText() => "Online";

        public string SubText() => _id.Name;

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
        
        [Command(nameof(DeleteCollection))]
        public async void Delete()
        {
            _view.SetText($"Removed {_id.Name}");
            SetControl(null);
            Header.IsVisible = false;
            
            OnlineStorage storage = OnlineStorage.Get();
            await storage.RemoveCollection(_id);
            
            ReloadTopBar?.Invoke();
        }

        [Command(nameof(ShareCollection))]
        public async Task Share()
        {
            await ClipboardService.SetTextAsync(_id.Id);
            await Utils.Utils.ShowMessageBox("Clipboard", "Copied share code to clipboard");
        }
    }
}