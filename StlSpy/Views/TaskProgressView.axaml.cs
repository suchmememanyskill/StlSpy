using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using StlSpy.Service;

namespace StlSpy.Views;

public partial class TaskProgressView : UserControl
{
    public TaskProgressView()
    {
        InitializeComponent();
        Loop();
    }

    private async void Loop()
    {
        while (true)
        {
            int totalTasks = AppTask.Tasks.Count;
            if (totalTasks > 0)
                SetTask(AppTask.Tasks.First(), totalTasks);
            else
                SetTask(null, 0);
            
            await Task.Delay(500);
        }
    }

    private void SetTask(AppTask? task, int totalTasks)
    {
        TaskName.IsVisible = ProgressBar.IsVisible = (task != null);
        
        if (task == null)
        {
            ExtraTasksCount.IsVisible = false;
            TaskProgress.IsVisible = true;
            TaskProgress.Content = "No Background Tasks";
        }
        else
        {
            ExtraTasksCount.IsVisible = totalTasks > 1;
            if (ExtraTasksCount.IsVisible)
            {
                ExtraTasksCount.Content =
                    (totalTasks == 2) ? "(+ 1 task)" : $"(+ {totalTasks - 1} tasks)";
            }

            TaskName.Content = task.Name;
            TaskProgress.IsVisible = ProgressBar.IsVisible = task.Progress != 0;
            if (TaskProgress.IsVisible)
            {
                TaskProgress.Content = task.TextProgress;
                ProgressBar.Value = task.Progress;
            }
        }
    }
}