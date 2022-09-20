using System;
using System.Collections.Generic;

namespace StlSpy.Model.PostsEndpoint;

public class Post : PreviewPost
{
    public string Description { get; set; }
    public List<ApiFile> Images { get; set; }
    public List<ApiFile> Downloads { get; set; }
    public DateTimeOffset Added { get; set; }
    public DateTimeOffset Modified { get; set; }
    public long DownloadCount { get; set; }
    public long LikeCount { get; set; }
}