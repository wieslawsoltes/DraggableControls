using Avalonia.Controls;
using Avalonia.Metadata;

namespace DraggableDemo.Behaviors
{
    public class SharedContent : Control
    {
        [Content]
        public object? Content { get; set; }
    }
}