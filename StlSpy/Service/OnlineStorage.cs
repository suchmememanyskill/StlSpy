using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Service;

public class OnlineStorage : ICollectionStorage
{
    private string _collectionsStoragePath;
    private static OnlineStorage? _instance;
    private Dictionary<string, string>? _onlineCollections;

    private OnlineStorage()
    {
        _collectionsStoragePath = Path.Join(Settings.ConfigPath, "online.json");
    }
    public static OnlineStorage Get() => _instance ??= new();
    
    private async Task<Dictionary<string, string>> Load()
    {
        if (_onlineCollections != null)
            return _onlineCollections;

        if (!File.Exists(_collectionsStoragePath))
            _onlineCollections = new();
        else
            _onlineCollections = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                await File.ReadAllTextAsync(_collectionsStoragePath))!;

        return _onlineCollections;
    }

    private async Task Save()
    {
        if (_onlineCollections == null)
            return;

        await File.WriteAllTextAsync(_collectionsStoragePath, JsonConvert.SerializeObject(_onlineCollections));
    }

    public async Task<GenericCollection?> GetPosts(CollectionId id)
    {
        var result = await UnifiedPrintApi.GetOnlineCollection(id.Id);
        return new(id.Id, result.CollectionName, result.Posts);
    }

    public async Task<List<CollectionId>> GetCollections()
        => (await Load()).Select(x => new CollectionId(x.Key, x.Value)).ToList();

    public async Task RemovePost(CollectionId id, string uid)
        => await UnifiedPrintApi.RemoveFromOnlineCollection(id.Id, uid);
    

    public async Task AddPost(CollectionId id, Post post)
        => await UnifiedPrintApi.AddToOnlineCollection(id.Id, post.UniversalId);

    public async Task<CollectionId> AddCollection(string name)
    {
        string token = await UnifiedPrintApi.NewOnlineCollection(name);
        var items = await Load();
        items.Add(token, name);
        await Save();
        return new(token, name);
    }

    public async Task RemoveCollection(CollectionId id)
    {
        (await Load()).Remove(id.Id);
        await Save();
    }

    public async Task<bool> IsPostPartOfCollection(string uid)
    {
        throw new NotImplementedException("Unsupported");
    }

    public async Task<bool> IsPostPartOfCollection(string uid, CollectionId id)
    {
        var posts = await UnifiedPrintApi.GetOnlineCollectionUids(id.Id);

        if (posts == null)
            return false;
        
        return posts.UIDs.Contains(uid);
    }

    public string Name() => "Online Collection";

    public async Task<string> ImportCollection(string token)
    {
        var items = await Load();
        if (items.ContainsKey(token))
            throw new Exception("Collection already exists");

        OnlineCollection x;

        try
        {
            x = await UnifiedPrintApi.GetOnlineCollection(token);
        }
        catch
        {
            throw new Exception("Collection does not exist");
        }
        
        (await Load()).Add(token, x.CollectionName);
        await Save();
        return x.CollectionName;
    }
}