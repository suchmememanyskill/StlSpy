using System;
using System.Collections.Generic;
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
        public Post Post { get; private set; }
        private int _imagePage = 0;
        public event Action<PostView> OnInitialised; 

        [Binding(nameof(PostName), "Content")] 
        public string PostNameText => Post.Name;

        [Binding(nameof(PostAuthor), "Content")]
        public string PostAuthorText => $"By {Post.Author.Name}, Published {Post.Added:MMMM dd, yyyy}";

        [Binding(nameof(PostDescription), "Text")]
        public string PostDescriptionText => Post.Description;

        [Binding(nameof(MainPanel), "IsVisible")]
        public bool Visible => Post != null;

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
            Post = post;
        }

        public async void GetPost(string uid)
        {
            Post? post = await UnifiedPrintApi.PostsUniversalId(uid);
            if (post != null)
            {
                Post = post;
                Init();
            }
        }

        public new void Init()
        {
            UpdateView();
            DownloadAuthorImage();
            DownloadPostImage();
            OnInitialised?.Invoke(this);
        }

        public void SetCustomisableButtons(List<IControl> controls)
        {
            CustomisableButtons.Children.Clear();
            CustomisableButtons.Children.AddRange(controls);
            SetCustomisableButtonsStatus(true);
        }

        public void SetCustomisableButtonsStatus(bool enabled)
        {
            CustomisableButtons.IsEnabled = enabled;
        }
        
        public async void DownloadAuthorImage()
        {
            try
            {
                byte[]? data = await Post.Author.Thumbnail.Get();
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
                byte[]? data = await Post.Images[_imagePage].Get();
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
        public void OpenPostUrl() => Utils.Utils.OpenUrl(Post.Website);
        
        [Command(nameof(PostAuthor))]
        public void OpenPostAuthorUrl() => Utils.Utils.OpenUrl(Post.Author.Website);

        [Command(nameof(LeftImageButton))]
        public void PreviousImagePage()
        {
            _imagePage--;
            if (_imagePage < 0)
                _imagePage = Post.Images.Count - 1;
            DownloadPostImage();
        }

        [Command(nameof(RightImageButton))]
        public void NextImagePage()
        {
            _imagePage++;
            _imagePage %= Post.Images.Count;
            DownloadPostImage();
        }

        [Command(nameof(CloseButton))]
        public void Close()
        {
            TopElement.IsVisible = false;
        }
    }
}