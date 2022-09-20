using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
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
            _apis = await UnifiedPrintApi.PostsServices();

            MenuButton menuButton = new(_apis.Select(x =>
            {
                return new Command(x.Name, x.SortTypes.Select(y => new Command(y.DisplayName, () => ChangeViewToSortType(x, y))).ToList());
            }), "Sites");
            
            StackPanel.Children.Add(menuButton);
            
            StackPanel.Children.Add(new MenuButton(new List<Command>(), "Search"));
            StackPanel.Children.Add(new MenuButton(new List<Command>(), "Collections"));
            StackPanel.Children.Add(new MenuButton(new List<Command>(), "Local"));

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

        public void ChangeViewToSortType(ApiDescription api, SortType sort)
        {
            SetContent(new SortTypeView(api, sort));
            HeaderBackground.Background = api.GetColorAsBrush();
        }
    }
}