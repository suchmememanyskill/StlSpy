using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Skia;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class SearchView : UserControlExt<SearchView>, IMainView
    {
        private ApiDescription? _api;
        private int _page = 1;
        private int _perPage = 20;
        private PreviewPostCollectionView _view;
        private string? _query;
        private PostView? _postView;
        
        public bool HasApi => (_api != null);

        [Binding(nameof(LeftArrow), "IsVisible")]
        [Binding(nameof(Page), "IsVisible")]
        [Binding(nameof(RightArrow), "IsVisible")]
        public bool HasResults => HasApi & !string.IsNullOrWhiteSpace(_query);
        
        public SearchView()
        {
            InitializeComponent();
            SetControls();
        }

        public SearchView(ApiDescription api) : this()
        {
            _api = api;
            _view = new();
            _view.OnNewSelection += x =>
            {
                _postView = new PostView(x.Post.UniversalId);
                _postView.OnInitialised += RespondToButtonRefresh;
                SetControl(_postView);
            };
            VerticalStackPanel.Children.Add(_view);
            SearchButton.Background = _api.GetColorAsBrush();
            Get();
            UpdateView();
        }

        public SearchView(bool _) : this()
        {
            _view = new();
            _view.OnNewSelection += x =>
            {
                _postView = new PostView(x.Post.UniversalId);
                _postView.OnInitialised += RespondToButtonRefresh;
                SetControl(_postView);
            };
            VerticalStackPanel.Children.Add(_view);
            Get();   
            UpdateView();
        }
        
        private async void Get()
        {
            if (!string.IsNullOrWhiteSpace(_query))
                _view.SetPosts((HasApi) ? GetApiPosts() : GetUidPosts());
            else
                _view.SetText("");
            
            UpdateView();
        }
        
        private async void SetButtonsOnPostView()
        {
            var addToLocalCollection = await Buttons.AddToCollection(_postView!, LocalStorage.Get(), RespondToButtonRefresh);
            var addToOnlineCollection = await Buttons.AddToCollection(_postView!, OnlineStorage.Get(), RespondToButtonRefresh);
            
            _postView?.SetCustomisableButtons(new()
            {
                Buttons.DownloadButton(_postView, RespondToButtonRefresh),
                Buttons.OpenPrusaSlicerButton(_postView, RespondToButtonRefresh),
                Buttons.OpenFolder(_postView, RespondToButtonRefresh),
                addToOnlineCollection,
                addToLocalCollection
            });
        }

        private void RespondToButtonRefresh(PostView post)
        {
            if (post == _postView)
                SetButtonsOnPostView();
        }

        public void SetControl(IControl control)
        {
            SidePanel.Children.Clear();
            SidePanel.Children.Add(control);
        }

        private async Task<List<PreviewPostView>> GetApiPosts()
        {
            LeftArrow.IsEnabled = false;
            RightArrow.IsEnabled = false;
            PreviewPostsCollection collection =
                await UnifiedPrintApi.PostsSearch(_api.Slug, _query!, _page, _perPage);
            if (collection.TotalResults >= 0)
            {
                long totalPages = (collection.TotalResults + _perPage - 1) / _perPage;
                RightArrow.IsEnabled = totalPages > _page;
                Page.Content = $"Page {_page}/{totalPages}";
            }
            else
            {
                Page.Content = $"Page {_page}";
                RightArrow.IsEnabled = collection.PreviewPosts.Count != 0;
            }
            
            LeftArrow.IsEnabled = (_page > 1);
            return collection.PreviewPosts.Select(x => new PreviewPostView(x, _api)).ToList();
        }

        private async Task<List<PreviewPostView>> GetUidPosts()
        {
            List<string> filteredUids = _query.Split(',').Where(x => x.Contains(':')).Select(x => x.Trim()).Distinct().ToList();
            List<Post> posts = new();

            foreach (var filteredUid in filteredUids)
            {
                try
                {
                    Post? post = await UnifiedPrintApi.PostsUniversalId(filteredUid);
                    if (post != null)
                        posts.Add(post);
                }
                catch
                { }
            }
            
            return posts.Select(x => new PreviewPostView(x, ApiDescription.GetLocalApiDescription())).ToList();
        }

        public string MainText() => (HasApi) ? _api.Name : "Universal ID";
        public string SubText() => "Search";
        public IBrush? HeaderColor() => (HasApi) ? _api.GetColorAsBrush() : ApiDescription.GetLocalApiDescription().GetColorAsBrush();

        [Command(nameof(SearchButton))]
        public void OnSearch()
        {
            _query = SearchBox.Text;
            Get();
        }
        
        [Command(nameof(LeftArrow))]
        public void OnLeftArrow()
        {
            _page--;
            Get();
        }
        
        [Command(nameof(RightArrow))]
        public void OnRightArrow()
        {
            _page++;
            Get();
        }
    }
}