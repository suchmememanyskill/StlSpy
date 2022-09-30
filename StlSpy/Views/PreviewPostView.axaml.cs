using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class PreviewPostView : UserControlExt<PreviewPostView>
    {
        public PreviewPost Post { get; }
        public ApiDescription Api { get; }
        private bool _downloadedImage = false;

        [Binding(nameof(Panel), "Background")] 
        [Binding(nameof(CheckboxBorder), "Background")]
        public IBrush Color => Api.GetColorAsBrush();

        [Binding(nameof(Title), "Content")] 
        public string PostName => Post.Name;

        public async void DownloadImage()
        {
            if (_downloadedImage)
                return;

            _downloadedImage = true;
            try
            {
                byte[]? data = await Post.Thumbnail.Get();
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
            EffectiveViewportChanged += EffectiveViewportChangedReact;

            CheckboxBorder.IsVisible = !Settings.Get().HidePrintedLabel;

            if (CheckboxBorder.IsVisible)
            {
                CheckBox.IsChecked = Settings.Get().ContainsPrintedUid(Post.UniversalId);
            
                CheckBox.Checked += (_, _) =>
                    Settings.Get().AddPrintedUid(Post.UniversalId);
            
                CheckBox.Unchecked += (_, _) =>
                    Settings.Get().RemovePrintedUid(Post.UniversalId);
            }
        }

        public PreviewPostView()
        {
            InitializeComponent();
        }
        
        private void EffectiveViewportChangedReact(object? obj, EffectiveViewportChangedEventArgs args)
        {
            if (args.EffectiveViewport.IsEmpty)
                return;
        
            EffectiveViewportChanged -= EffectiveViewportChangedReact;
            DownloadImage();
        }
    }
}