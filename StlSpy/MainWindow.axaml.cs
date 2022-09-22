using System.Collections.Generic;
using System.Linq;
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
            LocalStorage storage = LocalStorage.Get();
            if (!(await storage.GetCollectionNames()).Contains("Downloads"))
                await storage.CreateCollection("Downloads");

            _apis = await UnifiedPrintApi.PostsServices();

            MenuButton sites = new(_apis.Select(x =>
            {
                return new Command(x.Name, x.SortTypes.Select(y => new Command(y.DisplayName, () => ChangeViewToSortType(x, y))).ToList());
            }), "Sites");
            
            StackPanel.Children.Add(sites);

            MenuButton search = 
                new(_apis.Select(x => new Command(x.Name, () => ChangeViewToSearchType(x))), "Search");
            
            StackPanel.Children.Add(search);

            MenuButton localCollections = new((await storage.GetCollectionNames()).Select(x => new Command(x, () => ChangeViewToLocalCollectionsType(x))),
                "Local Collections");
            
            StackPanel.Children.Add(new MenuButton(new List<Command>(), "Collections"));
            StackPanel.Children.Add(localCollections);

            Label l = new();
            l.Content = "Please click one of the buttons above to get started";
            l.FontSize = 25;
            l.HorizontalAlignment = HorizontalAlignment.Center;
            l.VerticalAlignment = VerticalAlignment.Center;
            SetContent(l);
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
            SetView(new LocalCollectionView(collection));
        }
    }
}