using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using StlSpy.Views;

namespace StlSpy;

public partial class PostDetailsWindow : Window
{
    private PreviewPostView _preview;
    public Post? Post { get; private set; }
    private int _imagePage = 0;
    private bool _local = false;
    
    public PostDetailsWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public PostDetailsWindow(PreviewPostView previewPostView) : this()
    {
        _preview = previewPostView;
        Window.Title = $"{_preview.Post.Name} by {_preview.Post.Author.Name}";
        PostName.Command = new LambdaCommand(_ => OpenPostUrl());
        PostAuthor.Command = new LambdaCommand(_ => OpenPostAuthorUrl());
        LeftImageButton.Command = new LambdaCommand(_ => PreviousImagePage());
        RightImageButton.Command = new LambdaCommand(_ => NextImagePage());
        SetContent();
    }

    public async Task SetContent()
    {
        MainPanel.IsVisible = false;
        Post = await _preview.ConvertToPost();
        if (Post != null)
        {
            LocalStorage storage = LocalStorage.Get();
            _local = storage.AreFilesCached(Post.UniversalId);
            
            PostName.Content = Post.Name;
            PostAuthor.Content = 
                (_local) 
                    ? $"By {Post.Author.Name}, Added to collection on {Post.Added:MMMM dd, yyyy}" 
                    : $"By {Post.Author.Name}, Published {Post.Added:MMMM dd, yyyy}";
            PostDescription.Text = Post.Description;
            DownloadAuthorImage();
            DownloadPostImage();

            ExpandedMenuButton openIn = new(new List<Command>()
            {
                new("PrusaSlicer", () => _preview.OpenInPrusaSlicer()),
                new("Explorer", () => _preview.OpenInExplorer()),
                new("Browser", OpenPostUrl)
            }, "Open in");
            
            List<Command> localItems = await _preview.GetAddToCollectionList(LocalStorage.Get());
            localItems.RemoveAt(0);
            ExpandedMenuButton local = new(localItems, "Add/Remove to Local Collection");

            openIn.OnButtonPress += _ => OnMenuButtonPress();
            local.OnButtonPress += _ => OnMenuButtonPress();

            CustomisableButtons.Children.Clear();
            CustomisableButtons.Children.Add(openIn);
            CustomisableButtons.Children.Add(local);
            CustomisableButtons.IsEnabled = true;
        }
        MainPanel.IsVisible = true;
    }

    private async void OnMenuButtonPress()
    {
        CustomisableButtons.IsEnabled = false;
        await Task.Delay(1000); // Make sure the task has started by then
        AppTask task = new("Waiting until ready");
        await task.WaitUntilReady();
        task.Complete();
        await SetContent();
    }
    
    public async void DownloadAuthorImage()
    {
        if (AuthorImage.Source != null)
            return;
        
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
                LeftImageButton.IsVisible = RightImageButton.IsVisible = true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Exception] {e}");
        }
    }
    
    public void OpenPostUrl()
    {
        if (Post != null)
            Utils.Utils.OpenUrl(Post.Website);
    }
    
    public void OpenPostAuthorUrl()
    {
        if (Post != null)
            Utils.Utils.OpenUrl(Post.Author.Website);
    }
    
    public void PreviousImagePage()
    {
        _imagePage--;
        if (_imagePage < 0)
            _imagePage = Post.Images.Count - 1;
        DownloadPostImage();
    }
    
    public void NextImagePage()
    {
        _imagePage++;
        _imagePage %= Post.Images.Count;
        DownloadPostImage();
    }
}