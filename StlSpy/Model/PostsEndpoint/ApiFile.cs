using System;
using System.Threading.Tasks;
using StlSpy.Utils;

namespace StlSpy.Model.PostsEndpoint;

public class ApiFile
{
    public string Name { get; set; }
    public Uri Url { get; set; }

    public async Task<byte[]> Get()
    {
        return await Request.GetAsync(Url);
    }
}