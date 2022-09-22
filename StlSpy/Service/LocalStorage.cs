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
    private static LocalStorage? instance;
    private string _basePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpy");
    private List<Post>? _localPosts;
    private CollectionHolder? _localCollections;

    private LocalStorage(){}
    public static LocalStorage Get() => instance ??= new();
    
    private string GetPath(string uid)
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

    private async Task SavePostLocally(Post post)
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
        
        await GetAllLocalPosts(true);
    }

    private async Task<Post?> LocalToPost(string dataPath)
    {
        if (!File.Exists(dataPath))
            return null;

        LocalPost post = JsonConvert.DeserializeObject<LocalPost>(await File.ReadAllTextAsync(dataPath))!;
        return LocalToPost(post);
    }
    
    private Post LocalToPost(LocalPost local)
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

    public async Task<List<Post>> GetAllLocalPosts(bool force = false)
    {
        if (_localPosts != null && !force)
            return _localPosts;
        
        string basePath = Path.Join(_basePath, "Posts");

        if (!Directory.Exists(basePath))
            return new();


        List<Post?> posts = new();

        foreach (var x in Directory.EnumerateDirectories(basePath).Where(x => File.Exists(Path.Join(x, "data.json"))))
        {
            posts.Add(await LocalToPost(Path.Join(x, "data.json")));
        }

        _localPosts = posts.Where(x => x != null).Select(x => x!).ToList();
        return _localPosts;
    }

    private async Task<CollectionHolder> GetLocalCollections()
    {
        if (_localCollections != null)
            return _localCollections;
        
        string dataPath = Path.Join(_basePath, "local.json");
        if (!File.Exists(dataPath))
            _localCollections = new();
        else
            _localCollections = JsonConvert.DeserializeObject<CollectionHolder>(await File.ReadAllTextAsync(dataPath))!;
        
        return _localCollections;
    }

    private async Task SaveLocalCollections()
    {
        if (_localCollections == null)
            return;
        
        string dataPath = Path.Join(_basePath, "local.json");
        await File.WriteAllTextAsync(dataPath, JsonConvert.SerializeObject(_localCollections));
    }

    public async Task<List<Post>?> GetLocalPosts(string collection)
    {
        CollectionHolder? collections = await GetLocalCollections();
        Collection? col = collections?.Collections.Find(x => x.Name == collection);

        if (col == null)
            return null;

        List<Post> posts = await GetAllLocalPosts();
        return posts.Where(x => col.UIDs.Contains(x.UniversalId)).ToList();
    }

    public async Task DeleteLocalPost(string uid)
    {
        string fullPath = GetPath(uid);
        Directory.Delete(fullPath, true);
        await GetAllLocalPosts(true);
    }

    public async Task CreateCollection(string name)
    {
        CollectionHolder collection = await GetLocalCollections();

        if (collection.Collections.Any(x => x.Name == name))
            throw new Exception("Collection already exists!");
        
        collection.Collections.Add(new(name));
        await SaveLocalCollections();
    }

    public async Task AddToCollection(string name, Post post)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == name);

        if (collection == null)
            throw new Exception("Collection does not exist!");

        if (!AreFilesCached(post.UniversalId))
        {
            await SavePostLocally(post);
        }
        
        if (!collection.UIDs.Contains(post.UniversalId))
            collection.UIDs.Add(post.UniversalId);
        
        await SaveLocalCollections();
    }

    public async Task RemoveFromCollection(string name, string uid)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == name);

        if (collection == null)
            throw new Exception("Collection does not exist!");

        collection.UIDs.Remove(uid);
        if (!await IsPartOfAnyCollection(uid))
        {
            await DeleteLocalPost(uid);
        }
        
        await SaveLocalCollections();
    }

    public async Task RemoveCollection(string name)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == name);
        if (collection != null)
            collections.Collections.Remove(collection);
        
        await SaveLocalCollections();
    }

    public async Task<bool> IsPartOfAnyCollection(string uid)
    {
        var collections = await GetLocalCollections();
        return collections?.Collections.Any(x => x.UIDs.Contains(uid)) ?? false;
    }

    public async Task<bool> IsPartOfSpecificCollection(string collection, string uid)
    {
        var collections = await GetLocalCollections();
        return collections?.Collections.Find(x => x.Name == collection)?.UIDs.Contains(uid) ?? false;
    }
    
    public async Task<List<string>> GetCollectionNames()
    {
        CollectionHolder collections = await GetLocalCollections();
        return collections.Collections.Select(x => x.Name).ToList();
    }
}