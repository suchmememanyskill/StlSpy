using System;

namespace StlSpy.Model.PostsEndpoint;

public class Author
{
    public string Name { get; set; }
    public Uri Website { get; set; }
    public ApiFile Thumbnail { get; set; }
}