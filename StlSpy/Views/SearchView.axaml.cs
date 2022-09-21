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

namespace StlSpy.Views
{
    public partial class SearchView : UserControlExt<SearchView>, IMainView
    {
        private ApiDescription _api;
        private int _page = 1;
        private int _perPage = 20;
        private PreviewPostCollectionView _view;
        private string? _query;
        
        public SearchView()
        {
            InitializeComponent();
            SetControls();
        }

        public SearchView(ApiDescription api) : this()
        {
            _api = api;
            _view = new();
            VerticalStackPanel.Children.Add(_view);
            SearchButton.Background = _api.GetColorAsBrush();
            Get();
        }
        
        private async void Get()
        {
            if (!string.IsNullOrWhiteSpace(_query))
            {
                Page.IsVisible = RightArrow.IsVisible = LeftArrow.IsVisible = true;
                _view.SetPosts(GetPosts());
            }
            else
            {
                _view.SetText("");
                Page.IsVisible = RightArrow.IsVisible = LeftArrow.IsVisible = false;
            }
        }

        private async Task<List<PreviewPostView>> GetPosts()
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

        public string MainText() => _api.Name;
        public string SubText() => "Search";
        public IBrush? HeaderColor() => _api.GetColorAsBrush();

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