using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Utils;

namespace StlSpy.Service;

public class UnifiedPrintApi
{
    private static readonly string SITE = "https://vps.suchmeme.nl/print";

    public static async Task<List<ApiDescription>> PostsServices() =>
        JsonConvert.DeserializeObject<List<ApiDescription>>(
            await Request.GetStringAsync(new Uri($"{SITE}/Posts/services"), 5))!;

    public static async Task<PreviewPostsCollection> PostsList(string apiName, string sortType, int page = 1,
        int perPage = 20)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["page"] = page.ToString();
        parameters["perPage"] = perPage.ToString();
        return JsonConvert.DeserializeObject<PreviewPostsCollection>(
            await Request.GetStringAsync(new Uri($"{SITE}/Posts/list/{apiName}/{sortType}?{parameters}")))!;
    }

    public static async Task<PreviewPostsCollection> PostsSearch(string apiName, string query, int page = 1,
        int perPage = 20)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["page"] = page.ToString();
        parameters["perPage"] = perPage.ToString();
        parameters["query"] = query;
        return JsonConvert.DeserializeObject<PreviewPostsCollection>(
            await Request.GetStringAsync(new Uri($"{SITE}/Posts/list/{apiName}/search?{parameters}")))!;
    }

    public static async Task<Post?> PostsUniversalId(string uid)
    {
        try
        {
            return JsonConvert.DeserializeObject<Post>(
                await Request.GetStringAsync(new Uri($"{SITE}/Posts/universal/{uid}")))!;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<OnlineCollection> GetOnlineCollection(string token)
    {
        return JsonConvert.DeserializeObject<OnlineCollection>(
            await Request.GetStringAsync(new Uri($"{SITE}/Saved/{token}"), 500))!;
    }

    public static async Task<OnlineCollectionUids> GetOnlineCollectionUids(string token)
    {
        return JsonConvert.DeserializeObject<OnlineCollectionUids>(
            await Request.GetStringAsync(new Uri($"{SITE}/Saved/{token}/uids")))!;
    }
    
    public static async Task<string> NewOnlineCollection(string name)
    {
        return await Request.PostStringAsync(new Uri($"{SITE}/Saved"),
            $"{{\"collectionName\": {JsonConvert.SerializeObject(name)}}}");
    }

    public static async Task AddToOnlineCollection(string token, string uid)
    {
        await Request.PostStringAsync(new Uri($"{SITE}/Saved/{token}/add"),
            $"{{\"uid\": {JsonConvert.SerializeObject(uid)}}}");
    }

    public static async Task RemoveFromOnlineCollection(string token, string uid)
    {
        await Request.DeleteStringAsync(new Uri($"{SITE}/Saved/{token}/remove"),
            $"{{\"uid\": {JsonConvert.SerializeObject(uid)}}}");
    }
}
