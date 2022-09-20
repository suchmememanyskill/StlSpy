using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Views
{
    public partial class PreviewPostCollectionView : UserControl
    {
        public PreviewPostCollectionView()
        {
            InitializeComponent();
        }

        public void SetText(string text)
        {
            List.Items = new List<PreviewPostView>();
            Label.Content = text;
        }

        public void SetPosts(List<PreviewPostView> posts)
        {
            if (posts.Count <= 0)
            {
                SetText("No posts found");
                return;
            }

            List.Items = posts;
            Label.Content = "";
        }

        public async void SetPosts(Task<List<PreviewPostView>> postsTask)
        {
            SetText("Loading...");
            SetPosts(await postsTask);
        }
    }
}