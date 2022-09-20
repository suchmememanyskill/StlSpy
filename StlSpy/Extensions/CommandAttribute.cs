using System;

namespace StlSpy.Extensions
{
    public class CommandAttribute : Attribute
    {
        public string ButtonName { get; set; }

        public CommandAttribute(string buttonName) => ButtonName = buttonName;
    }
}