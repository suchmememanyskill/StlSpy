using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class SortTypeView : UserControlExt<SortTypeView>
    {
        private ApiDescription _api;
        private SortType _sort;
        private PreviewPostCollectionView _view;
        private int page = 1;
        private int perPage = 20;

        public SortTypeView(ApiDescription apiDescription, SortType sortType)
        {
            _api = apiDescription;
            _sort = sortType;
            InitializeComponent();
            SetControls();
            _view = new();
            VerticalStackPanel.Children.Add(_view);
            Get();
        }

        public SortTypeView()
        {
            InitializeComponent();
        }

        private async void Get()
        {
            SortTypeLabel.Content = $"On {_api.Name}, Sorting {_sort.DisplayName}";
            _view.SetPosts(GetPosts());
        }

        private async Task<List<PreviewPostView>> GetPosts()
        {
            LeftArrow.IsEnabled = false;
            RightArrow.IsEnabled = false;
            PreviewPostsCollection collection = await UnifiedPrintApi.PostsList(_api.Slug, _sort.UriName, page, perPage);
            if (collection.TotalResults >= 0)
            {
                long totalPages = collection.TotalResults / perPage;
                RightArrow.IsEnabled = totalPages > page;
                Page.Content = $"Page {page}/{totalPages}";
            }
            else
            {
                Page.Content = $"Page {page}";
                RightArrow.IsEnabled = collection.PreviewPosts.Count != 0;
            }
            
            LeftArrow.IsEnabled = (page > 1);
            return collection.PreviewPosts.Select(x => new PreviewPostView(x, _api)).ToList();
        }

        [Command(nameof(LeftArrow))]
        public void OnLeftArrow()
        {
            page--;
            Get();
        }
        
        [Command(nameof(RightArrow))]
        public void OnRightArrow()
        {
            page++;
            Get();
        }
    }
}