using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views
{
    public partial class PostView : UserControlExt<PostView>
    {
        private Post _post;
        private int _imagePage = 0;

        [Binding(nameof(PostName), "Content")] 
        public string PostNameText => _post.Name;

        [Binding(nameof(PostAuthor), "Content")]
        public string PostAuthorText => $"By {_post.Author.Name}, Published {_post.Added:MMMM dd, yyyy}";

        [Binding(nameof(PostDescription), "Text")]
        public string PostDescriptionText => _post.Description;

        [Binding(nameof(MainPanel), "IsVisible")]
        public bool Visible => _post != null;
        
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
            DownloadAuthorImage();
            DownloadPostImage();
        }
        
        public async void DownloadAuthorImage()
        {
            try
            {
                byte[]? data = await _post.Author.Thumbnail.Get();
                if (data != null)
                {
                    Stream stream = new MemoryStream(data);
                    AuthorImage.Source = Bitmap.DecodeToWidth(stream, 50);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception] {e}");
            }
        }

        public async void DownloadPostImage()
        {
            try
            {
                byte[]? data = await _post.Images[_imagePage].Get();
                if (data != null)
                {
                    Stream stream = new MemoryStream(data);
                    MainImage.Source = Bitmap.DecodeToWidth(stream, 790);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception] {e}");
            }
        }

        [Command(nameof(PostName))]
        public void OpenPostUrl() => Utils.Utils.OpenUrl(_post.Website);
        
        [Command(nameof(PostAuthor))]
        public void OpenPostAuthorUrl() => Utils.Utils.OpenUrl(_post.Author.Website);

        [Command(nameof(LeftImageButton))]
        public void PreviousImagePage()
        {
            _imagePage--;
            if (_imagePage < 0)
                _imagePage = _post.Images.Count - 1;
            DownloadPostImage();
        }

        [Command(nameof(RightImageButton))]
        public void NextImagePage()
        {
            _imagePage++;
            _imagePage %= _post.Images.Count;
            DownloadPostImage();
        }
    }
}