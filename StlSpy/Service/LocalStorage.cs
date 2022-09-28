﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Service;

public class LocalStorage : ICollectionStorage
{
    public static readonly CollectionId DEFAULT_DOWNLOAD_LOCATION = new("Downloads", "Downloads");
    
    private static LocalStorage? instance;
    private string _basePath;
    private List<Post>? _localPosts;
    private CollectionHolder? _localCollections;

    private LocalStorage()
    {
        _basePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpy");

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }
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
        {
            byte[]? thumbnail = await post.Thumbnail.Get();
            if (thumbnail != null)
                await File.WriteAllBytesAsync(thumbnailPath, thumbnail);
        }
        
        string authorPath = Path.Join(fullPath, "author.jpg");
        if (!File.Exists(authorPath))
        {
            byte[]? thumbnail = await post.Author.Thumbnail.Get();
            if (thumbnail != null)
                await File.WriteAllBytesAsync(authorPath, thumbnail);
        }

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

        string authorThumbnailPath = Path.Join(fullPath, "author.jpg");
        Author author = new()
        {
            Name = local.AuthorName,
            Website = local.AuthorSite,
            Thumbnail = new()
            {
                Name = "author.jpg",
                FullFilePath = File.Exists(authorThumbnailPath) ? authorThumbnailPath : null
            }
        };

        string postThumbnailPath = Path.Join(fullPath, "thumbnail.jpg");
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
                FullFilePath = File.Exists(postThumbnailPath) ? postThumbnailPath : null
            }
        };
        return post;
    }

    private async Task<List<Post>> GetAllLocalPosts(bool force = false)
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
    
    public async Task DeleteLocalPost(string uid)
    {
        string fullPath = GetPath(uid);
        Directory.Delete(fullPath, true);
        await GetAllLocalPosts(true);
    }

    public async Task<List<Post>> GetPosts()
        => await GetAllLocalPosts();

    public async Task<GenericCollection?> GetPosts(CollectionId id)
    {
        CollectionHolder? collections = await GetLocalCollections();
        Collection? col = collections?.Collections.Find(x => x.Name == id.Name);

        if (col == null)
            return null;

        List<Post> posts = await GetAllLocalPosts();
        return new(id, posts.Where(x => col.UIDs.Contains(x.UniversalId)).ToList());
    }

    public async Task<List<CollectionId>> GetCollections()
        => (await GetLocalCollections()).Collections.Select(x => new CollectionId(x.Name, x.Name)).ToList();

    public async Task RemovePost(CollectionId id, string uid)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == id.Name);

        if (collection == null)
            throw new Exception("Collection does not exist!");

        collection.UIDs.Remove(uid);
        if (!await IsPostPartOfCollection(uid))
        {
            await DeleteLocalPost(uid);
        }
        
        await SaveLocalCollections();
    }

    public async Task AddPost(CollectionId id, Post post)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == id.Name);

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

    public async Task<CollectionId> AddCollection(string name)
    {
        CollectionHolder collection = await GetLocalCollections();

        if (collection.Collections.Any(x => x.Name == name))
            throw new Exception("Collection already exists!");
        
        collection.Collections.Add(new(name));
        await SaveLocalCollections();
        return new(name, name);
    }

    public async Task RemoveCollection(CollectionId id)
    {
        CollectionHolder collections = await GetLocalCollections();
        Collection? collection = collections.Collections.Find(x => x.Name == id.Name);
        if (collection != null)
            collections.Collections.Remove(collection);
        
        await SaveLocalCollections();
    }

    public async Task<bool> IsPostPartOfCollection(string uid)
    {
        var collections = await GetLocalCollections();
        return collections?.Collections.Any(x => x.UIDs.Contains(uid)) ?? false;
    }

    public async Task<bool> IsPostPartOfCollection(string uid, CollectionId id)
    {
        var collections = await GetLocalCollections();
        return collections?.Collections.Find(x => x.Name == id.Name)?.UIDs.Contains(uid) ?? false;
    }

    public string Name() => "Local Collection";
}