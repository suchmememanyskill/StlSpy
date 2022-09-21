﻿using System;
using System.IO;
using System.Threading.Tasks;
using StlSpy.Utils;

namespace StlSpy.Model.PostsEndpoint;

public class ApiFile
{
    public string Name { get; set; }
    public Uri? Url { get; set; }
    public string? FullFilePath { get; set; }

    public async Task<byte[]> Get()
    {
        if (FullFilePath != null)
            return await File.ReadAllBytesAsync(FullFilePath);
        
        return await Request.GetAsync(Url!);
    }
}