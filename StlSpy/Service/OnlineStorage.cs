using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Service;

public class OnlineStorage
{
    private string _basePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpy");
    private string _collectionsStoragePath;
    private static OnlineStorage? _instance;
    private Dictionary<string, string>? _onlineCollections;

    private OnlineStorage()
    {
        _collectionsStoragePath = Path.Join(_basePath, "online.json");
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

    public async Task<Dictionary<string, string>> GetCollections()
    {
        return await Load();
    }

    public async Task<List<Post>> GetPosts(string token)
    {
        return (await UnifiedPrintApi.GetOnlineCollection(token)).Posts;
    }

    public async Task AddPost(string token, string uid)
    {
        await UnifiedPrintApi.AddToOnlineCollection(token, uid);
    }

    public async Task RemoveFromCollection(string token, string uid)
    {
        await UnifiedPrintApi.RemoveFromOnlineCollection(token, uid);
    }

    public async Task<bool> IsPostPartOfCollection(string token, string uid)
    {
        var posts = await GetPosts(token);
        return posts.Any(x => x.UniversalId == uid);
    }

    public async Task<string> CreateCollection(string name)
    {
        string token = await UnifiedPrintApi.NewOnlineCollection(name);
        var items = await Load();
        items.Add(token, name);
        await Save();
        return token;
    }

    public async Task RemoveCollection(string token)
    {
        (await Load()).Remove(token);
        await Save();
    }

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