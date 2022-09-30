using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace StlSpy.Service;

public interface IMainView : IControl
{
    public string MainText();
    public string SubText();
    public IBrush? HeaderColor();

    public void RegisterTopBarRefreshHandle(Action refresh)
    {
    }
}