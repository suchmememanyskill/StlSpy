using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace StlSpy.Model.PostsEndpoint;

public class SortType
{
    public string DisplayName { get; set; }
    public string UriName { get; set; }
    public string DisplayDescription { get; set; }
}

public class ApiDescription
{
    public string Name { get; set; }
    public string Color { get; set; }
    public List<SortType> SortTypes { get; set; }
    public Uri Site { get; set; }
    public string Description { get; set; }
    public string Slug { get; set; }

    private IBrush? _colorCache;

    public IBrush GetColorAsBrush()
    {
        Color color = Avalonia.Media.Color.Parse(Color);
        return _colorCache ??= new SolidColorBrush(color);
    }
}