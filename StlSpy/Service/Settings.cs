using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace StlSpy.Service;

public class Settings
{
    private static Settings? _settings;

    public static Settings Get()
    {
        if (_settings == null)
        {
            string path = Path.Join(ConfigPath, "settings.json");

            if (!File.Exists(path))
                return new();

            _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path))!;
        }

        return _settings;
    }

    public static string ConfigPath
    {
        get
        {
            string path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpy");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }
    }

    public string CustomLocalCollectionsPath { get; set; } = "";
    public bool HidePrintedLabel { get; set; } = false;
    public List<string> PrintedUids { get; set; } = new();

    public List<string> GetLocalCollectionPaths()
    {
        string defaultPath = Path.Join(ConfigPath, "Posts");

        if (!Directory.Exists(defaultPath))
            Directory.CreateDirectory(defaultPath);

        List<string> paths = new();

        if (!string.IsNullOrWhiteSpace(CustomLocalCollectionsPath) && Directory.Exists(CustomLocalCollectionsPath))
        {
            paths.Add(CustomLocalCollectionsPath);
        }
        
        paths.Add(defaultPath);

        return paths;
    }

    public void Save()
    {
        string path = Path.Join(ConfigPath, "settings.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(this));
    }

    public void AddPrintedUid(string uid)
    {
        if (!PrintedUids.Contains(uid))
        {
            PrintedUids.Add(uid);
            Save();
        }
    }

    public void RemovePrintedUid(string uid)
    {
        if (PrintedUids.Contains(uid))
        {
            PrintedUids.Remove(uid);
            Save();
        }
    }

    public bool ContainsPrintedUid(string uid) => PrintedUids.Contains(uid);
}