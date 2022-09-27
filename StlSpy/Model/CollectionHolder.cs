using System.Collections.Generic;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Model;

public record CollectionId(string Id, string Name);

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

public class GenericCollection
{
    public CollectionId Name;
    public List<Post> Posts;

    public GenericCollection(string id, string name, List<Post>? posts = null)
    {
        Posts = posts ?? new();
        Name = new(id, name);
    }

    public GenericCollection(string name, List<Post>? posts = null) : this(name, name, posts)
    {}

    public GenericCollection(CollectionId id, List<Post>? posts) : this(id.Id, id.Name, posts)
    {}
}