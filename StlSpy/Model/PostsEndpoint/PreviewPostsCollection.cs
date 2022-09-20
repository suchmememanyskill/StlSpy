using System.Collections.Generic;

namespace StlSpy.Model.PostsEndpoint;

public class PreviewPostsCollection
{
    public List<PreviewPost> PreviewPosts { get; set; }
    public long TotalResults { get; set; }
}