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
    public partial class SortTypeView : UserControlExt<SortTypeView>, IMainView
    {
        private ApiDescription _api;
        private SortType _sort;
        private PreviewPostCollectionView _view;
        private int _page = 1;
        private int _perPage = 20;

        public SortTypeView(ApiDescription apiDescription, SortType sortType)
        {
            _api = apiDescription;
            _sort = sortType;
            InitializeComponent();
            SetControls();
            _view = new();
            _view.OnNewSelection += x =>
            {
                SetControl(new PostView(x.Post.UniversalId));
            };
            VerticalStackPanel.Children.Add(_view);
            Get();
        }

        public SortTypeView()
        {
            InitializeComponent();
        }

        private async void Get()
        {
            _view.SetPosts(GetPosts());
        }

        private async Task<List<PreviewPostView>> GetPosts()
        {
            LeftArrow.IsEnabled = false;
            RightArrow.IsEnabled = false;
            PreviewPostsCollection collection = await UnifiedPrintApi.PostsList(_api.Slug, _sort.UriName, _page, _perPage);
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
        
        public void SetControl(IControl control)
        {
            SidePanel.Children.Clear();
            SidePanel.Children.Add(control);
        }

        public string MainText() => _api.Name;
        public string SubText() => _sort.DisplayName;
        public IBrush? HeaderColor() => _api.GetColorAsBrush();
    }
}