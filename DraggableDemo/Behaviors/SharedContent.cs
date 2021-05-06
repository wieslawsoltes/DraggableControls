using Avalonia.Controls;
using Avalonia.Metadata;

namespace DockDemo.Behaviors
{
    public class SharedContent : Control
    {
        [Content]
        public object? Content { get; set; }
    }
}