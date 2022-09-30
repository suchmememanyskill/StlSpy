using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StlSpy.Service;

public class AppTask
{
    public static List<AppTask> Tasks { get; private set; } = new();
    public string Name { get; private set; }
    public string TextProgress
    {
        get => (string.IsNullOrWhiteSpace(_textProgress)) ? $"{Progress:0.0}%" : _textProgress;
        set => _textProgress = value;
    }
    public float Progress { get; set; } // 0 -> 100
    private string _textProgress = "";
    
    public AppTask(string name)
    {
        Name = name;
        Tasks.Add(this);
    }

    public async Task WaitUntilReady()
    {
        while (true)
        {
            if (Tasks.First() == this)
                return;

            await Task.Delay(500);
        }
    }
    
    public void Complete()
    {
        Tasks.Remove(this);
    }
}