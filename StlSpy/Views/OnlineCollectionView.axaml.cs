using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using TextCopy;

namespace StlSpy.Views
{
    public partial class OnlineCollectionView : UserControlExt<OnlineCollectionView>, IMainView
    {
        private string _collectionName;
        private string _token;
        private PreviewPostCollectionView _view;
        private PostView? _postView;

        [Binding(nameof(DeleteCollection), "Content")]
        public string DeleteButtonLabel => $"Remove Collection '{_collectionName}'";
        
        [Binding(nameof(ShareCollection), "Content")]
        public string ShareButtonLabel => $"Share Collection '{_collectionName}'";
        
        public event Action? ReloadTopBar;

        public OnlineCollectionView(string collectionName, string token) : this()
        {
            _collectionName = collectionName;
            _token = token;
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
        }

        public OnlineCollectionView()
        {
            InitializeComponent();
        }
        
        private async void SetButtonsOnPostView()
        {
            var addToLocalCollection = await Buttons.AddToLocalCollection(_postView!, RespondToButtonRefresh);
            var addToOnlineCollection = await Buttons.AddToOnlineCollection(_postView!, RespondToButtonRefresh);
            
            _postView?.SetCustomisableButtons(new()
            {
                Buttons.CreateButton($"Remove from {_collectionName}", OnRemove),
                Buttons.OpenPrusaSlicerButton(_postView, RespondToButtonRefresh),
                Buttons.OpenFolder(_postView, RespondToButtonRefresh),
                addToOnlineCollection,
                addToLocalCollection
            });
        }

        private async void OnRemove()
        {
            _postView?.SetCustomisableButtonsStatus(false);

            OnlineStorage storage = OnlineStorage.Get();
            await storage.RemoveFromCollection(_token, _postView?.Post.UniversalId ?? "");
            
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
            return (await storage.GetPosts(_token)).Select(x => new PreviewPostView(x, ApiDescription.GetLocalApiDescription())).ToList();
        }
        
        public void SetControl(IControl? control)
        {
            SidePanel.Children.Clear();
            if (control != null)
                SidePanel.Children.Add(control);
        }

        public string MainText() => "Online";

        public string SubText() => _collectionName;

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
        
        [Command(nameof(DeleteCollection))]
        public async void Delete()
        {
            _view.SetText($"Removed {_collectionName}");
            SetControl(null);
            Header.IsVisible = false;
            
            OnlineStorage storage = OnlineStorage.Get();
            await storage.RemoveCollection(_token);
            
            ReloadTopBar?.Invoke();
        }

        [Command(nameof(ShareCollection))]
        public async Task Share()
        {
            await ClipboardService.SetTextAsync(_token);
            await Utils.Utils.ShowMessageBox("Clipboard", "Copied share code to clipboard");
        }
    }
}