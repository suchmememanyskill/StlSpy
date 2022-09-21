using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using StlSpy.Model.PostsEndpoint;
using StlSpy.Utils;

namespace StlSpy.Service;

public class UnifiedPrintApi
{
    private static readonly string SITE = "http://152.70.57.126:8520";
    
    public static async Task<List<ApiDescription>> PostsServices() =>
        JsonConvert.DeserializeObject <List<ApiDescription>>(await Request.GetStringAsync(new Uri($"{SITE}/Posts/services")))!;

    public static async Task<PreviewPostsCollection> PostsList(string apiName, string sortType, int page = 1, int perPage = 20)
    {
        var parameters = HttpUtility.ParseQueryString(string.Empty);
        parameters["page"] = page.ToString();
        parameters["perPage"] = perPage.ToString();
        return JsonConvert.DeserializeObject<PreviewPostsCollection>(
            await Request.GetStringAsync(new Uri($"{SITE}/Posts/list/{apiName}/{sortType}?{parameters}")))!;
    }

    public static async Task<PreviewPostsCollection> PostsSearch(string apiName, string query, int page = 1, int perPage = 20)
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
}