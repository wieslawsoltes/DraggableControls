using System.Diagnostics;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace DockDemo.Behaviors
{
    public class TabItemDragBehavior : Behavior<TabItem>
    {
        private bool _enableDrag;
        private Point _start;
        private int _draggedIndex;
        private int _targetIndex;
        private TabControl? _tabControl;
        private TabItem? _draggedTabItem;

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is { })
            {
                AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, Released, RoutingStrategies.Tunnel);
                AssociatedObject.AddHandler(InputElement.PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
                AssociatedObject.AddHandler(InputElement.PointerMovedEvent, Moved, RoutingStrategies.Tunnel);
            }
        }
        
        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject is { })
            {
                AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, Released);
                AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, Pressed);
                AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, Moved);
            }
        }

        private void Pressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject?.Parent is not TabControl tabControl)
            {
                return;
            }

            _enableDrag = true;
            _start = e.GetPosition(AssociatedObject.Parent);
            _draggedIndex = -1;
            _targetIndex = -1;
            _tabControl = tabControl;
            _draggedTabItem = AssociatedObject;
            AddTransforms(_tabControl);
        }

        private void Released(object? sender, PointerReleasedEventArgs e)
        {
            if (_enableDrag)
            {
                RemoveTransforms(_tabControl);

                if (_draggedIndex >= 0 && _targetIndex >= 0 && _draggedIndex != _targetIndex)
                {
                    Debug.WriteLine($"MoveTabItem {_draggedIndex} -> {_targetIndex}");
                    MoveTabItem(_tabControl, _draggedIndex, _targetIndex);
                }

                _draggedIndex = -1;
                _targetIndex = -1;
                _enableDrag = false;
                _tabControl = null;
                _draggedTabItem = null;
            }
        }

        private void AddTransforms(TabControl? tabControl)
        {
            if (tabControl?.Items is not AvaloniaList<object> tabItems)
            {
                return;
            }

            for (var i = 0; i < tabItems.Count; i++)
            {
                if (tabItems[i] is TabItem tabItem)
                {
                    tabItem.RenderTransform = new TranslateTransform();
                }
            }  
        }

        private void RemoveTransforms(TabControl? tabControl)
        {
            if (tabControl?.Items is not AvaloniaList<object> tabItems)
            {
                return;
            }

            for (var i = 0; i < tabItems.Count; i++)
            {
                if (tabItems[i] is TabItem tabItem)
                {
                    tabItem.RenderTransform = null;
                }
            }  
        }

        private void MoveTabItem(TabControl? tabControl, int draggedIndex, int targetIndex)
        {
            if (tabControl?.Items is not AvaloniaList<object> tabItems)
            {
                return;
            }

            var tabItem = tabItems[draggedIndex];
            tabItems.RemoveAt(draggedIndex);
            tabItems.Insert(targetIndex, tabItem);
        }

        private void Moved(object? sender, PointerEventArgs e)
        {
            if (_tabControl is null || _draggedTabItem is null || !_enableDrag)
            {
                return;
            }

            if (_tabControl.Items is not AvaloniaList<object> tabItems)
            {
                return;
            }

            var position = e.GetPosition(_tabControl);
            var deltaX = position.X - _start.X;

            ((TranslateTransform) _draggedTabItem.RenderTransform).X = deltaX;

            _draggedIndex = _tabControl.ItemContainerGenerator.IndexFromContainer(_draggedTabItem);
            _targetIndex = -1;

            var draggedBounds = _draggedTabItem.Bounds;
            var draggedStartX = draggedBounds.X;
            var draggedDeltaStartX = draggedBounds.X + deltaX;
            var draggedDeltaEndX = draggedBounds.X + deltaX + draggedBounds.Width;

            for (var i = 0; i < tabItems.Count; i++)
            {
                if (tabItems[i] is not TabItem tabItem || tabItem.RenderTransform is null || ReferenceEquals(tabItem, _draggedTabItem))
                {
                    continue;
                }

                var targetBounds = tabItem.Bounds;
                var targetStartX = targetBounds.X;
                var targetMidX = targetBounds.X + targetBounds.Width / 2;
                var tabItemIndex = _tabControl.ItemContainerGenerator.IndexFromContainer(tabItem);

                if (targetStartX > draggedStartX && draggedDeltaEndX >= targetMidX)
                {
                    ((TranslateTransform) tabItem.RenderTransform).X = -draggedBounds.Width;
                    _targetIndex = _targetIndex == -1 ? tabItemIndex : tabItemIndex > _targetIndex ? tabItemIndex : _targetIndex;
                    Debug.WriteLine($"Moved Right {_draggedIndex} -> {_targetIndex}");
                }
                else if (targetStartX < draggedStartX && draggedDeltaStartX <= targetMidX)
                {
                    ((TranslateTransform) tabItem.RenderTransform).X = draggedBounds.Width;
                    _targetIndex = _targetIndex == -1 ? tabItemIndex : tabItemIndex < _targetIndex ? tabItemIndex : _targetIndex;
                    Debug.WriteLine($"Moved Left {_draggedIndex} -> {_targetIndex}");
                }
                else
                {
                    ((TranslateTransform) tabItem.RenderTransform).X = 0;
                }
            }

            Debug.WriteLine($"Moved {_draggedIndex} -> {_targetIndex}");
        }
    }
}