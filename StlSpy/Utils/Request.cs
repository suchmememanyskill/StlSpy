using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace StlSpy.Utils
{
    public static class Request
    {
        public static byte[] Get(Uri uri) => Get(uri, new());

        public static async Task<byte[]> GetAsync(Uri uri) => await GetAsync(uri, new());

        public static byte[] Get(Uri uri, Dictionary<string, string> headers)
        {
            using (var client = new WebClient())
            {
                foreach (var kv in headers)
                    client.Headers[kv.Key] = kv.Value;
                return client.DownloadData(uri);
            }
                
        }

        public static async Task<byte[]> GetAsync(Uri uri, Dictionary<string, string> headers)
        {
            using (HttpClient client = new())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                foreach (var kv in headers)
                    client.DefaultRequestHeaders.Add(kv.Key, kv.Value);

                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public static string GetString(Uri uri) => GetString(uri, new());

        public static async Task<string> GetStringAsync(Uri uri) => await GetStringAsync(uri, new());

        public static string GetString(Uri uri, Dictionary<string, string> headers)
        {
            using (var client = new WebClient())
            {
                foreach (var kv in headers)
                    client.Headers[kv.Key] = kv.Value;
                return client.DownloadString(uri);
            }

        }

        public static async Task<string> GetStringAsync(Uri uri, Dictionary<string, string> headers)
        {
            using (var client = new WebClient())
            {
                foreach (var kv in headers)
                    client.Headers[kv.Key] = kv.Value;

                return await client.DownloadStringTaskAsync(uri);
            }
        }

        public static async Task<string> PostStringAsync(Uri uri, string data)
        {
            using (var client = new WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                return await client.UploadStringTaskAsync(uri, data);
            }
        }
        
        public static async Task<string> DeleteStringAsync(Uri uri, string data)
        {
            using (var client = new WebClient())
            {
                client.Headers["Content-Type"] = "application/json";
                return await client.UploadStringTaskAsync(uri, "DELETE", data);
            }
        }
    }
}
