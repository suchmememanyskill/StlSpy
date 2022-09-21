using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Service;

public class LocalStorage
{
    private string _basePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpy");

    public string GetPath(string uid)
    {
        string fullPath = Path.Join(_basePath, "Posts", uid.Replace(':', '_'));
        
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        return fullPath;
    }
    
    public async Task<string?> GetFilesPath(Post post)
    {
        string fullPath = Path.Join(GetPath(post.UniversalId), "Files");
        
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        foreach (var x in post.Downloads)
        {
            string individualPath = Path.Join(fullPath, x.Name);
            if (!File.Exists(individualPath))
                await File.WriteAllBytesAsync(individualPath, await x.Get());
        }

        return fullPath;
    }

    public bool AreFilesCached(string uid)
    {
        string fullPath = Path.Join(_basePath, "Posts", uid.Replace(':', '_'), "Files");
        return Directory.Exists(fullPath);
    }

    public async Task<string?> GetImagesPath(Post post)
    {
        string fullPath = Path.Join(GetPath(post.UniversalId), "Images");
        
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        foreach (var x in post.Images)
        {
            string individualPath = Path.Join(fullPath, x.Name);
            if (!File.Exists(individualPath))
                await File.WriteAllBytesAsync(individualPath, await x.Get());
        }

        return fullPath;
    }

    public async Task SavePostLocally(Post post)
    {
        string fullPath = GetPath(post.UniversalId);

        await GetFilesPath(post);
        await GetImagesPath(post);

        string thumbnailPath = Path.Join(fullPath, "thumbnail.jpg");
        if (!File.Exists(thumbnailPath))
            await File.WriteAllBytesAsync(thumbnailPath, await post.Thumbnail.Get());

        string authorPath = Path.Join(fullPath, "author.jpg");
        if (!File.Exists(authorPath))
            await File.WriteAllBytesAsync(authorPath, await post.Author.Thumbnail.Get());

        LocalPost local = new(post);

        string dataPath = Path.Join(fullPath, "data.json");
        if (!File.Exists(dataPath))
            await File.WriteAllTextAsync(dataPath, JsonConvert.SerializeObject(local));
    }

    public async Task<Post?> LocalToPost(string dataPath)
    {
        if (!File.Exists(dataPath))
            return null;

        LocalPost post = JsonConvert.DeserializeObject<LocalPost>(await File.ReadAllTextAsync(dataPath))!;
        return LocalToPost(post);
    }
    
    public Post LocalToPost(LocalPost local)
    {
        string fullPath = GetPath(local.UniversalId);

        List<ApiFile> images = Directory.EnumerateFiles(Path.Join(fullPath, "Images")).Select(x => new ApiFile()
        {
            Name = Path.GetFileName(x),
            FullFilePath = x
        }).ToList();

        Author author = new()
        {
            Name = local.Name,
            Website = local.AuthorSite,
            Thumbnail = new()
            {
                Name = "author.jpg",
                FullFilePath = Path.Join(fullPath, "author.jpg")
            }
        };

        Post post = new()
        {
            Added = local.Added,
            Author = author,
            Description = local.Description,
            DownloadCount = local.DownloadCount,
            Downloads = new(),
            Id = local.Id,
            Images = images,
            LikeCount = local.LikeCount,
            Modified = local.Modified,
            Name = local.Name,
            UniversalId = local.UniversalId,
            Website = local.Website,
            Thumbnail = new()
            {
                Name = "thumbnail.jpg",
                FullFilePath = Path.Join(fullPath, "thumbnail.jpg")
            }
        };
        return post;
    }

    public async Task<List<Post>> LoadAllLocalPosts()
    {
        string basePath = Path.Join(_basePath, "Posts");

        if (!Directory.Exists(basePath))
            return new();


        List<Post?> posts = new();

        foreach (var x in Directory.EnumerateDirectories(basePath).Where(x => File.Exists(Path.Join(x, "data.json"))))
        {
            posts.Add(await LocalToPost(Path.Join(x, "data.json")));
        }

        return posts.Where(x => x != null).Select(x => x!).ToList();
    }

    public async Task<CollectionHolder?> GetLocalCollections()
    {
        string dataPath = Path.Join(_basePath, "local.json");
        if (!File.Exists(dataPath))
            return null;

        return JsonConvert.DeserializeObject<CollectionHolder>(await File.ReadAllTextAsync(dataPath));
    }

    public async Task<List<Post>?> LoadPostsBasedOnCollection(string collection)
    {
        CollectionHolder? collections = await GetLocalCollections();
        Collection? col = collections?.Collections.Find(x => x.Name == collection);

        if (col == null)
            return null;

        List<Post> posts = await LoadAllLocalPosts();
        return posts.Where(x => col.UIDs.Contains(x.UniversalId)).ToList();
    }

    public void DeleteLocalPost(string uid)
    {
        string fullPath = GetPath(uid);
        Directory.Delete(fullPath, true);
    }

    public async Task CreateCollection(string name)
    {
        
    }

    public async Task AddToCollection(string name, string uid)
    {
        
    }
}