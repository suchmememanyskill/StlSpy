using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class NewPostView : UserControl, IMainView
    {
        LocalStorage _storage = LocalStorage.Get();
        
        public NewPostView()
        {
            InitializeComponent();
            Init();
        }

        public async void Init()
        {
            List<CollectionId> collections = await _storage.GetCollections();
            MenuButton button = new(collections.Select(x => new Command(x.Name, () => Add(x))).ToList(),
                "Add to Local Collection");
            
            button.SetFontSize(14);
            button.HorizontalContentAlignment = HorizontalAlignment.Center;
            StackPanel.Children.Add(button);
            
            StackPanel.Children.Add(new TextBlock()
            {
                Text = "After creating the collection, you need to add some files to it to complete the collection:\n- 'thumbnail.jpg': Adds a thumbnail image to the post\n- 'author.jpg': Adds a thumbnail image to the author\n- 'Images/*': Any image in this folder gets loaded\n- 'Files/*': Any STL, OBJ or 3MF file gets loaded",
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        public async void Add(CollectionId id)
        {
            OnError("");
            string? name = Name.Text;
            string? website = Website.Text;
            string? authorName = AuthorName.Text;
            string? authorWebsite = AuthorWebsite.Text;
            string? description = Description?.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                OnError("Name is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(website))
            {
                OnError("Website is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(authorName))
            {
                OnError("Author Name is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(authorWebsite))
            {
                OnError("Author Website is empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                OnError("Description is empty");
                return;
            }

            try
            {
                Post post = await _storage.NewLocalPost(name, website, authorName, authorWebsite, description);
                await _storage.AddPost(id, post);
                string path = await _storage.GetFilesPath(post);
                path = Path.GetDirectoryName(path);
                
                File.WriteAllText(Path.Join(path, "thumbnail.jpg.placeholder"), "");
                File.WriteAllText(Path.Join(path, "author.jpg.placeholder"), "");
                
                Utils.Utils.OpenFolder(path!);
                MainWindow.Window?.SetView(new LocalCollectionView(id));
            }
            catch (Exception e)
            {
                OnError("Description is empty");
            }
        }

        private void OnError(string err)
        {
            ErrorLabel.Content = err;
            StackPanel.IsEnabled = err != "";
        }

        public string MainText() => "Create";

        public string SubText() => "Custom Post";

        public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
    }
}