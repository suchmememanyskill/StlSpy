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

namespace StlSpy.Views
{
    public partial class LocalCollectionView : UserControlExt<LocalCollectionView>, IMainView
    {
        private string _collectionName;
        private PreviewPostCollectionView _view;
        private PostView? _postView;

        public event Action? ReloadTopBar;

        [Binding(nameof(DeleteCollection), "Content")]
        public string DeleteButtonLabel => $"Remove Collection '{_collectionName}'";

        public LocalCollectionView(string collectionName) : this()
        {
            _collectionName = collectionName;
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

            DeleteCollection.IsVisible = collectionName != "Downloads";
        }

        public LocalCollectionView()
        {
            InitializeComponent();
        }
        
        private async void SetButtonsOnPostView()
        {
            var addToLocalCollection = await Buttons.AddToLocalCollection(_postView!, RespondToButtonRefresh);
            
            _postView?.SetCustomisableButtons(new()
            {
                Buttons.CreateButton($"Remove from {_collectionName}", OnRemove),
                Buttons.OpenPrusaSlicerButton(_postView, RespondToButtonRefresh),
                Buttons.OpenFolder(_postView, RespondToButtonRefresh),
                addToLocalCollection
            });
        }

        private async void OnRemove()
        {
            _postView?.SetCustomisableButtonsStatus(false);

            LocalStorage storage = LocalStorage.Get();
            await storage.RemoveFromCollection(_collectionName, _postView?.Post.UniversalId ?? "");
            
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
            return (await storage.GetLocalPosts(_collectionName)).Select(x => new PreviewPostView(x, ApiDescription.GetLocalApiDescription())).ToList();
        }
        
        public void SetControl(IControl? control)
        {
            SidePanel.Children.Clear();
            if (control != null)
                SidePanel.Children.Add(control);
        }

        public string MainText() => "Local";

        public string SubText() => _collectionName;

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(DeleteCollection))]
        public async void Delete()
        {
            _view.SetText($"Removing {_collectionName}...");
            SetControl(null);
            DeleteCollection.IsVisible = false;

            LocalStorage storage = LocalStorage.Get();
            List<Post>? posts = await storage.GetLocalPosts(_collectionName);

            if (posts == null)
            {
                _view.SetText($"Failed to remove {_collectionName}...");
                return;
            }

            await storage.RemoveCollection(_collectionName);
            
            foreach (var post in posts)
            {
                if (!await storage.IsPartOfAnyCollection(post.UniversalId))
                    await storage.DeleteLocalPost(post.UniversalId);
            }

            _view.SetText($"Removed {_collectionName}");
            ReloadTopBar?.Invoke();
        }
    }
}