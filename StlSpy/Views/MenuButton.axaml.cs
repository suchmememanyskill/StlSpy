using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using StlSpy.Extensions;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class MenuButton : UserControl
    {
        private List<TemplatedControl> _items = new();

        public MenuButton()
        {
            InitializeComponent();
        }
        
        public MenuButton(string header)
            : this()
        {
            Menu.Header = header;
            Menu.ItemsSource = _items = new List<TemplatedControl>();
        }

        public MenuButton(IEnumerable<TemplatedControl> items, string header)
            : this(header)
        {
            Menu.ItemsSource = _items = items.ToList();
        }

        public MenuButton(IEnumerable<Command> items, string header)
            : this(items.Select(x => x.ToTemplatedControl()), header)
        {
        }

        public void Add(Command command)
        {
            _items.Add(command.ToTemplatedControl());
            Menu.ItemsSource = null;
            Menu.ItemsSource = _items;
        }

        public void SetFontSize(double fontSize) => Menu.FontSize = fontSize;
    }
}