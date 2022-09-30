using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Service;

namespace StlSpy.Model;

public class LocalPost
{
    public string Id { get; set; }
    public string UniversalId { get; set; }
    public string Name { get; set; }
    public Uri Website { get; set; }
    public Uri AuthorSite { get; set; }
    public string AuthorName { get; set; }
    
    public string Description { get; set; }
    public DateTimeOffset Added { get; set; }
    public DateTimeOffset Modified { get; set; }
    public long DownloadCount { get; set; }
    public long LikeCount { get; set; }
    
    [JsonIgnore]
    public string? FolderPath { get; set; }

    public LocalPost() {}

    public LocalPost(Post post)
    {
        Id = post.Id;
        UniversalId = post.UniversalId;
        Name = post.Name;
        Website = post.Website;
        AuthorSite = post.Author.Website;
        AuthorName = post.Author.Name;
        Description = post.Description;
        Added = DateTimeOffset.Now;
        Modified = post.Modified;
        DownloadCount = post.DownloadCount;
        LikeCount = post.LikeCount;
    }
}