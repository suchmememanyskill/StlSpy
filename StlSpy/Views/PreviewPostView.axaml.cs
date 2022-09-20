using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class PreviewPostView : UserControlExt<PreviewPostView>
    {
        public PreviewPost Post { get; }
        public ApiDescription Api { get; }

        [Binding(nameof(Panel), "Background")] 
        public IBrush Color => Api.GetColorAsBrush();

        [Binding(nameof(Title), "Content")] 
        public string PostName => Post.Name;

        public async void DownloadImage()
        {
            try
            {
                byte[]? data = await Request.GetAsync(Post.Thumbnail);
                if (data != null)
                {
                    Stream stream = new MemoryStream(data);
                    Background!.Source = Bitmap.DecodeToWidth(stream, 300);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Exception] {e}");
            }
        }
        
        public PreviewPostView(PreviewPost previewPost, ApiDescription api)
        {
            
            Post = previewPost;
            Api = api;
            InitializeComponent();
            SetControls();
            UpdateView();
            DownloadImage();
        }

        public PreviewPostView()
        {
            InitializeComponent();
        }
    }
}