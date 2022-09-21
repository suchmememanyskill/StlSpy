using System;

namespace StlSpy.Model.PostsEndpoint;

public class PreviewPost
{
    public string Id { get; set; }
    public string UniversalId { get; set; }
    public string Name { get; set; }
    public ApiFile Thumbnail { get; set; }
    public Uri Website { get; set; }
    public Author Author { get; set; }
}