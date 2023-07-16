using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace StlSpy.Service;

public interface IMainView
{
    public string MainText();
    public string SubText();
    public IBrush? HeaderColor();
}