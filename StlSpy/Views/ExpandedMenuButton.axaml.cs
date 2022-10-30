using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using StlSpy.Extensions;
using StlSpy.Utils;

namespace StlSpy.Views;

public partial class ExpandedMenuButton : UserControl
{
    private List<Control> _items = new();
        
    public ExpandedMenuButton()
    {
        InitializeComponent();
    }
        
    public ExpandedMenuButton(string header)
        : this()
    {
        Header.Content = header;
    }

    public ExpandedMenuButton(IEnumerable<Control> items, string header)
        : this(header)
    {
        _items = items.ToList();
        Items.Children.AddRange(_items.ToList());
    }

    public ExpandedMenuButton(IEnumerable<Command> items, string header)
        : this(items.Select(CommandToControl), header)
    {
    }

    private static Control CommandToControl(Command c)
    {
        switch (c.Type)
        {
            case CommandType.Separator:
                return new Rectangle()
                {
                    Fill = Brushes.Black,
                    Margin = new(5, 4),
                    Height = 1,
                };
            case CommandType.SubMenu:
                MenuButton button = new MenuButton(c.SubCommands, c.Text!)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TopMenu =
                    {
                        Background = Brushes.Transparent,
                        Height = 27,
                    },
                    Menu =
                    {
                        FontSize = 14,
                        Padding = new(3)
                    }
                };

                return button;
            case CommandType.Text:
                return new Label()
                {
                    Content = c.Text
                };
            case CommandType.Function:
                return new Button()
                {
                    Content = c.Text,
                    Command = new LambdaCommand(_ => c.Action?.Invoke()),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Background = Brushes.Transparent,
                    FontSize = 14,
                    Padding = new(3),
                };
            default:
                throw new NotImplementedException();
        }
    }
}