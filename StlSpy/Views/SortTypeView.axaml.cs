using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class SortTypeView : UserControl
    {
        private ApiDescription _api;
        private SortType _sort;

        public SortTypeView(ApiDescription apiDescription, SortType sortType)
        {
            _api = apiDescription;
            _sort = sortType;
            InitializeComponent();
            Get();
        }

        public SortTypeView()
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