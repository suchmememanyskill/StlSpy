using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MsBox.Avalonia;
using StlSpy.Extensions;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Views;

public partial class SearchViewMerged : UserControlExt<SearchViewMerged>, IMainView
{
    public string MainText() => "Search";
    public string SubText() => "All Sites";
    public IBrush? HeaderColor() => ApiDescription.GetLocalApiDescription().GetColorAsBrush();
    
    private List<ApiDescription> _apis;
    private List<ApiDescription> _allApis;
    private int _perPage = 20;

    private string _query = "";
    private Dictionary<string, int> _page = new();
    private List<PreviewPostView> _views = new();

    public SearchViewMerged()
    {
        InitializeComponent();
        SetControls();
    }

    public SearchViewMerged(List<ApiDescription> apis) : this()
    {
        _apis = _allApis = apis;

        ApiSelect.ItemsSource = new List<ComboBoxItem>()
        {
            new ComboBoxItem()
            {
                Content = "All"
            }
        }.Concat(apis.Select(x => new ComboBoxItem()
        {
            Content = x.Name
        })).ToList();

        ApiSelect.SelectedIndex = 0;
        ApiSelect.SelectionChanged += (sender, args) =>
        {
            string name = (ApiSelect.SelectedItem as ComboBoxItem).Content as string;
            _apis = (name == "All") ? _allApis : _allApis.Where(x => x.Name == name).ToList();
                
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                View.SetText("");
                More.IsVisible = false;
            }
            else
            {
                Search();
            }
        };
    }

    private async Task<List<PreviewPostView>> GetResults()
    {
        List<string> split = SearchBox.Text!.Split(",").Select(x =>
        {
            if (x.StartsWith("https://www.thingiverse.com/thing:"))
                return $"thingiverse:{x.Split(':').Last()}";
            if (x.StartsWith("https://www.myminifactory.com/object/"))
                return $"myminifactory:{x.Split('-').Last()}";
            if (x.StartsWith("https://www.printables.com/model"))
                return $"prusa-printables:{x.Split('/').Last().Split('-').First()}";

            return x;
        }).ToList();
        
        List<string> uids = split.Where(x => x.Contains(":")).Select(x => x.Trim()).Distinct().ToList();
        if (uids.Count > 0 && split.Count != uids.Count)
        {
            await MessageBoxManager.GetMessageBoxStandard("Failed to search", "Cannot combine UID search and normal search").ShowAsync();
            return new();
        }

        if (uids.Count > 0)
        {
            More.IsVisible = false;
            List<PreviewPostView> posts = new();
            foreach (var filteredUid in uids)
            {
                try
                {
                    Post? post = await UnifiedPrintApi.PostsUniversalId(filteredUid);

                    if (post == null)
                        continue;

                    ApiDescription? description = _apis.Find(x => post.UniversalId.StartsWith(x.Slug));
                    description ??= ApiDescription.GetLocalApiDescription();
                    posts.Add(new PreviewPostView(post, description));
                }
                catch
                { }
            }

            return posts;
        }

        if (split.Count != 1)
            return new();

        if (!string.IsNullOrEmpty(_query) && _views.Count > 0 && split[0] != _query)
            return new();
        
        _query = split[0];
        More.IsVisible = false;
        
        if (_views.Count <= 0)
            _apis.ForEach(x => _page[x.Name] = 1);
        
        foreach (var x in _apis)
        {
            if (!_page.ContainsKey(x.Name))
                continue;

            var collection = await UnifiedPrintApi.PostsSearch(x.Slug, _query!, _page[x.Name]++, _perPage);
            
            if (collection.TotalResults >= 0)
            {
                if (collection.TotalResults <= _perPage * (_page[x.Name] - 1))
                    _page.Remove(x.Name);
                else
                    collection.PreviewPosts.ForEach(y => _views.Add(new(y, x)));
            }
            else
            {
                if (collection.PreviewPosts.Count <= 0)
                    _page.Remove(x.Name);
                else
                    collection.PreviewPosts.ForEach(y => _views.Add(new(y, x)));
            }
        }

        More.IsVisible = _page.Count > 0;
        
        return _views;
    }
    
    [Command(nameof(SearchButton))]
    public async void Search()
    {
        _query = "";
        _page = new();
        _views = new();
        await View.SetPosts(GetResults());
        View.CountLabel.IsVisible = false;
    }

    [Command(nameof(More))]
    public async void ViewMore()
    {
        Vector offset = ScrollViewer.Offset;
        await View.SetPosts(GetResults());
        ScrollViewer.Offset = offset;
        View.CountLabel.IsVisible = false;
    }
}