using System.Collections.Generic;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Model;

public class CollectionHolder
{
    public List<Collection> Collections { get; set; } = new();
}

public class Collection
{
    public string Name { get; set; }
    public List<string> UIDs { get; set; } = new();
    
    public Collection(){}

    public Collection(string name)
    {
        Name = name;
    }
}

public class OnlineCollection
{
    public string CollectionName { get; set; }
    public List<Post> Posts { get; set; }
}