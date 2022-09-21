using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class PostView : UserControlExt<PostView>
    {
        private Post _post;

        [Binding(nameof(PostName), "Content")] 
        public string PostNameText => _post.Name;

        [Binding(nameof(PostAuthor), "Content")]
        public string PostAuthorText => $"By {_post.Author.Name}, Published {_post.Added:MMMM dd, yyyy}";
        
        public PostView()
        {
            InitializeComponent();
            SetControls();
        }

        public PostView(string uid) : this()
        {
            GetPost(uid);
        }

        public PostView(Post post) : this()
        {
            _post = post;
        }

        public async void GetPost(string uid)
        {
            Post? post = await UnifiedPrintApi.PostsUniversalId(uid);
            if (post != null)
            {
                _post = post;
                Init();
            }
        }

        public new void Init()
        {
            UpdateView();
        }

        [Command(nameof(PostName))]
        public void OpenPostUrl() => Utils.Utils.OpenUrl(_post.Website);
        
        [Command(nameof(PostAuthor))]
        public void OpenPostAuthorUrl() => Utils.Utils.OpenUrl(_post.Author.Website);
    }
}