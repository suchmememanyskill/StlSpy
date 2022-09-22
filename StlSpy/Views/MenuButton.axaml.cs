using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using StlSpy.Extensions;
using StlSpy.Utils;

namespace StlSpy.Views
{
    public partial class MenuButton : UserControl
    {
        public MenuButton()
        {
            InitializeComponent();
        }
        
        public MenuButton(string header)
            : this()
        {
            Menu.Header = header;
        }

        public MenuButton(IEnumerable<TemplatedControl> items, string header)
            : this(header)
        {
            Menu.Items = items.ToList();
        }

        public MenuButton(IEnumerable<Command> items, string header)
            : this(items.Select(x => x.ToTemplatedControl()), header)
        {
        }

        public void SetFontSize(double fontSize) => Menu.FontSize = fontSize;
    }
}