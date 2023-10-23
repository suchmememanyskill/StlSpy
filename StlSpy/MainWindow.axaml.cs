using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using StlSpy.Views;

namespace StlSpy
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Window { get; private set; }
        
        private List<ApiDescription> _apis;
        public MainWindow()
        {
            Window = this;
            InitializeComponent();
            Init();
        }

        public async void Init()
        {
            SetTopButtons();

            Label l = new();
            l.Content = "Please click one of the buttons on the left to get started";
            l.FontSize = 25;
            l.HorizontalAlignment = HorizontalAlignment.Center;
            l.VerticalAlignment = VerticalAlignment.Center;
            SetContent(l);
            
            LocalStorage storage = LocalStorage.Get();
            AppTask task = new("Loading posts from disk");
            await storage.GetPosts();
            task.Complete();
        }

        public async void SetTopButtons()
        {
            StackPanel.Children.Clear();
            
            LocalStorage localStorage = LocalStorage.Get();
            OnlineStorage onlineStorage = OnlineStorage.Get();
            if ((await localStorage.GetCollections()).All(x => x.Name != "Downloads"))
                await localStorage.AddCollection("Downloads");

            try
            {
                _apis = await UnifiedPrintApi.PostsServices();
            }
            catch
            {
                _apis = new();
            }

            if (_apis.Count > 0)
            {
                ExpandedMenuButton sites = new(_apis.Select(x =>
                {
                    return new Command(x.Name, x.SortTypes.Select(y => new Command(y.DisplayName, () => ChangeViewToSortType(x, y))).ToList());
                }).Concat(new List<Command>()
                {
                    new (),
                    new ("Search", () => ChangeViewToSearchMerged())
                }), "Sites");
            
                StackPanel.Children.Add(sites);
            }
            
            if (_apis.Count > 0)
            {
                List<Command> onlineCollectionItems = (await onlineStorage.GetCollections())
                    .Select(x => new Command(x.Name, () => OnImportCollectionViaShareId(x.Id))).ToList();
                
                if (onlineCollectionItems.Count > 0)
                    onlineCollectionItems.Add(new());
                
                onlineCollectionItems.Add(new("Import Share ID", () => ChangeViewToNewCollectionView(OnImportCollectionViaShareId, "Import Collection", "from code", "Enter Share ID Here", "Import Share")));
                
                StackPanel.Children.Add(new ExpandedMenuButton(onlineCollectionItems, "Shared Collections"));
            }

            List<Command> localCollectionItems = (await localStorage.GetCollections())
                .Select(x => new Command(x.Name, () => ChangeViewToLocalCollectionsType(x))).ToList();
            
            localCollectionItems.Add(new());
            localCollectionItems.Add(new("All", () => ChangeViewToLocalCollectionsType(new CollectionId("ALL", "All"))));
            localCollectionItems.Add(new("New Collection", () => ChangeViewToNewCollectionView(x => OnNewCollection(x, LocalStorage.Get(), false))));
            localCollectionItems.Add(new("New Custom Post", () => SetView(new NewPostView())));
            

            StackPanel.Children.Add(new ExpandedMenuButton(localCollectionItems, "Local Collections"));

            StackPanel.Children.Add(new ExpandedMenuButton(new List<Command>(){new("Settings", () => SetView(new SettingsView()))}, "Misc"));
        }

        public void SetContent(Control control)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(control);
        }

        public void SetView(IMainView view)
        {
            SetContent(view as Control);
            IBrush? brush = view.HeaderColor();
            if (brush != null)
                HeaderBackground.Background = brush;

            MainText.Content = view.MainText();
            SubText.Content = view.SubText();
        }

        public void ChangeViewToSortType(ApiDescription api, SortType sort)
        {
            SetView(new SortTypeView(api, sort));
        }

        public void ChangeViewToSearchType(ApiDescription? api)
        {
            if (api == null)
                SetView(new SearchView(true));
            else
                SetView(new SearchView(api));
        }

        public void ChangeViewToSearchMerged()
        {
            SetView(new SearchViewMerged(_apis));
        }

        public void ChangeViewToLocalCollectionsType(CollectionId id)
        {
            var x = new LocalCollectionView(id);
            x.ReloadTopBar += SetTopButtons;
            SetView(x);
        }

        public void ChangeViewToNewCollectionView(Func<string, Task<string?>> onSubmit)
        {
            SetView(new NewCollectionView(onSubmit));
        }
        
        public void ChangeViewToNewCollectionView(Func<string, Task<string?>> onSubmit, string mainText, string subText,
            string watermarkText, string submitButtonText)
        {
            SetView(new NewCollectionView(onSubmit, mainText, subText, watermarkText, submitButtonText));
        }

        public async Task<string?> OnNewCollection(string collectionName, ICollectionStorage storage, bool online)
        {
            CollectionId id;
            try
            {
                id = await storage.AddCollection(collectionName);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            
            SetTopButtons();

            if (!online)
                ChangeViewToLocalCollectionsType(id);
            
            return null;
        }

        private async Task<string?> OnImportCollectionViaShareId(string input)
        {
            input = input.Trim();
            AppTask task = new("Validating Share ID");
            await task.WaitUntilReady();

            OnlineStorage onlineStorage = OnlineStorage.Get();
            GenericCollection? collection = string.IsNullOrEmpty(input)
                ? null
                : await onlineStorage.GetPosts(new CollectionId(input, ""));

            if (collection == null)
                return "Shared Collection not found!";

            OnlineCollectionView view = new(collection.Name);
            view.ReloadTopBar += SetTopButtons;
            SetView(view);
            task.Complete();
            return null;
        }
    }
}