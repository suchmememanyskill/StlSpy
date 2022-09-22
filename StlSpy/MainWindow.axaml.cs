using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;
using StlSpy.Utils;
using StlSpy.Views;

namespace StlSpy
{
    public partial class MainWindow : Window
    {
        private List<ApiDescription> _apis;
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public async void Init()
        {
            SetTopButtons();

            Label l = new();
            l.Content = "Please click one of the buttons above to get started";
            l.FontSize = 25;
            l.HorizontalAlignment = HorizontalAlignment.Center;
            l.VerticalAlignment = VerticalAlignment.Center;
            SetContent(l);
        }

        public async void SetTopButtons()
        {
            StackPanel.Children.Clear();
            
            LocalStorage localStorage = LocalStorage.Get();
            OnlineStorage onlineStorage = OnlineStorage.Get();
            if (!(await localStorage.GetCollectionNames()).Contains("Downloads"))
                await localStorage.CreateCollection("Downloads");

            _apis = await UnifiedPrintApi.PostsServices();

            MenuButton sites = new(_apis.Select(x =>
            {
                return new Command(x.Name, x.SortTypes.Select(y => new Command(y.DisplayName, () => ChangeViewToSortType(x, y))).ToList());
            }), "Sites");
            
            StackPanel.Children.Add(sites);

            MenuButton search = 
                new(_apis.Select(x => new Command(x.Name, () => ChangeViewToSearchType(x))), "Search");
            
            StackPanel.Children.Add(search);

            List<Command> onlineCollectionItems = (await onlineStorage.GetCollections())
                .Select(x => new Command(x.Value, () => ChangeViewToOnlineCollectionsType(x.Value, x.Key))).ToList();

            onlineCollectionItems.Add(new());
            onlineCollectionItems.Add(new("New Collection", () => ChangeViewToNewCollectionView(OnNewOnlineCollection)));
            onlineCollectionItems.Add(new("Import Collection"));
            
            StackPanel.Children.Add(new MenuButton(onlineCollectionItems, "Online Collections"));
            
            List<Command> localCollectionItems = (await localStorage.GetCollectionNames())
                .Select(x => new Command(x, () => ChangeViewToLocalCollectionsType(x))).ToList();
            
            localCollectionItems.Add(new());
            localCollectionItems.Add(new("New Collection", () => ChangeViewToNewCollectionView(OnNewLocalCollection)));
            
            StackPanel.Children.Add(new MenuButton(localCollectionItems, "Local Collections"));
        }

        public void SetContent(IControl control)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(control);
        }

        public void SetView(IMainView view)
        {
            SetContent(view);
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

        public void ChangeViewToSearchType(ApiDescription api)
        {
            SetView(new SearchView(api));
        }

        public void ChangeViewToLocalCollectionsType(string collection)
        {
            var x = new LocalCollectionView(collection);
            x.ReloadTopBar += SetTopButtons;
            SetView(x);
        }
        
        public void ChangeViewToOnlineCollectionsType(string name, string token)
        {
            var x = new OnlineCollectionView(name, token);
            x.ReloadTopBar += SetTopButtons;
            SetView(x);
        }

        public void ChangeViewToNewCollectionView(Func<string, Task<string?>> onSubmit)
        {
            SetView(new NewCollectionView(onSubmit));
        }

        public async Task<string?> OnNewLocalCollection(string collectionName)
        {
            LocalStorage storage = LocalStorage.Get();
            if ((await storage.GetCollectionNames()).Contains(collectionName))
            {
                return "Collection already exists";
            }

            await storage.CreateCollection(collectionName);
            SetTopButtons();
            ChangeViewToLocalCollectionsType(collectionName);
            return null;
        }
        
        public async Task<string?> OnNewOnlineCollection(string collectionName)
        {
            OnlineStorage storage = OnlineStorage.Get();

            string token = await storage.CreateCollection(collectionName);
            SetTopButtons();
            ChangeViewToOnlineCollectionsType(collectionName, token);
            return null;
        }
    }
}