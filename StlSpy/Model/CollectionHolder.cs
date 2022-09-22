using System.Collections.Generic;

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