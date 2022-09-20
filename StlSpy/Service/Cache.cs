using System;
using System.IO;
using System.Threading.Tasks;

namespace StlSpy.Service;

public class Storage
{
    public async static Task<byte[]?> Cache(string filename, Func<Task<byte[]?>> data)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;
        
        foreach (char x in Path.GetInvalidFileNameChars())
        {
            if (filename.Contains(x))
                throw new Exception("Invalid file name detected!");
        }

        string tempPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StlSpyCache");
        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        string path = Path.Join(tempPath, filename);
        if (File.Exists(path))
            return await File.ReadAllBytesAsync(path);

        byte[]? generatedData = await data();

        if (generatedData == null)
            return null;
        
        await File.WriteAllBytesAsync(path, generatedData);
        return generatedData;
    }
}