using System;
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
        public event Action<PreviewPostView>? OnNewSelection;
        private List<PreviewPostView> _posts = new();
        
        public PreviewPostCollectionView()
        {
            InitializeComponent();
            List.SelectionChanged += List_SelectionChanged;
        }

        public void SetText(string text)
        {
            List.Items = _posts = new();
            Label.Content = text;
            Label.IsVisible = !string.IsNullOrWhiteSpace(text);
            CountLabel.IsVisible = false;
        }

        public void SetPosts(List<PreviewPostView> posts)
        {
            if (posts.Count <= 0)
            {
                SetText("No posts found");
                return;
            }

            List.Items = _posts = posts;
            Label.Content = "";
            Label.IsVisible = false;
            CountLabel.IsVisible = true;
            CountLabel.Content = $"Found {_posts.Count} posts";
        }

        public async void SetPosts(Task<List<PreviewPostView>> postsTask)
        {
            SetText("Loading...");
            SetPosts(await postsTask);
        }

        public void Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _posts.ForEach(x => x.IsVisible = true);
                return;
            }

            query = query.ToLower();
            
            _posts.ForEach(x => x.IsVisible = (x.Post.Author.Name.ToLower().Contains(query) || x.Post.Name.ToLower().Contains(query)));
        }

        private void List_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            PreviewPostView? view = List.SelectedItem as PreviewPostView;
            
            if (view != null)
                OnNewSelection?.Invoke(view);
        }
    }
}