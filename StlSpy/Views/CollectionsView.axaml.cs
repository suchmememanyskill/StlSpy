using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class CollectionsView : UserControl
    {
        private ApiDescription _api;
        private SortType _sort;

        public CollectionsView(ApiDescription apiDescription, SortType sortType)
        {
            _api = apiDescription;
            _sort = sortType;
            InitializeComponent();
            Get();
        }

        public CollectionsView()
        {
            InitializeComponent();
        }

        private async void Get()
        {
            var response = await UnifiedPrintApi.PostsList(_api.Slug, _sort.UriName);
            List.Items = response.PreviewPosts.Select(x => new PreviewPostView(x, _api));
        }
    }
}