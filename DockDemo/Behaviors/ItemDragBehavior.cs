using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace DockDemo.Behaviors
{
    public class ItemDragBehavior : Behavior<IControl>
    {
        private bool _enableDrag;
        private Point _start;
        private int _draggedIndex;
        private int _targetIndex;
        private ItemsControl? _itemsControl;
        private IControl? _draggedContainer;

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
            if (AssociatedObject?.Parent is not ItemsControl itemsControl)
            {
                return;
            }

            _enableDrag = true;
            _start = e.GetPosition(AssociatedObject.Parent);
            _draggedIndex = -1;
            _targetIndex = -1;
            _itemsControl = itemsControl;
            _draggedContainer = AssociatedObject;

            AddTransforms(_itemsControl);
        }

        private void Released(object? sender, PointerReleasedEventArgs e)
        {
            if (_enableDrag)
            {
                RemoveTransforms(_itemsControl);

                if (_draggedIndex >= 0 && _targetIndex >= 0 && _draggedIndex != _targetIndex)
                {
                    Debug.WriteLine($"MoveItem {_draggedIndex} -> {_targetIndex}");
                    MoveDraggedItem(_itemsControl, _draggedIndex, _targetIndex);
                }

                _draggedIndex = -1;
                _targetIndex = -1;
                _enableDrag = false;
                _itemsControl = null;
                _draggedContainer = null;
            }
        }

        private void AddTransforms(ItemsControl? itemsControl)
        {
            if (itemsControl?.Items is null)
            {
                return;
            }

            var i = 0;

            foreach (var _ in itemsControl.Items)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (container is not null)
                {
                    container.RenderTransform = new TranslateTransform();
                }
  
                i++;
            }  
        }

        private void RemoveTransforms(ItemsControl? itemsControl)
        {
            if (itemsControl?.Items is null)
            {
                return;
            }

            var i = 0;

            foreach (var _ in itemsControl.Items)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (container is not null)
                {
                    container.RenderTransform = null;
                }
  
                i++;
            }  
        }

        private void MoveDraggedItem(ItemsControl? itemsControl, int draggedIndex, int targetIndex)
        {
            if (itemsControl?.Items is not IList<object> items)
            {
                return;
            }

            var draggedItem = items[draggedIndex];
            items.RemoveAt(draggedIndex);
            items.Insert(targetIndex, draggedItem);
        }

        private void Moved(object? sender, PointerEventArgs e)
        {
            if (_itemsControl?.Items is null || _draggedContainer is null || !_enableDrag)
            {
                return;
            }

            var position = e.GetPosition(_itemsControl);
            var deltaX = position.X - _start.X;

            ((TranslateTransform) _draggedContainer.RenderTransform).X = deltaX;

            _draggedIndex = _itemsControl.ItemContainerGenerator.IndexFromContainer(_draggedContainer);
            _targetIndex = -1;

            var draggedBounds = _draggedContainer.Bounds;
            var draggedStartX = draggedBounds.X;
            var draggedDeltaStartX = draggedBounds.X + deltaX;
            var draggedDeltaEndX = draggedBounds.X + deltaX + draggedBounds.Width;

            var i = 0;

            foreach (var _ in _itemsControl.Items)
            {
                var targetContainer = _itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
                if (targetContainer?.RenderTransform is null || ReferenceEquals(targetContainer, _draggedContainer))
                {
                    i++;
                    continue;
                }

                var targetBounds = targetContainer.Bounds;
                var targetStartX = targetBounds.X;
                var targetMidX = targetBounds.X + targetBounds.Width / 2;
                var targetIndex = _itemsControl.ItemContainerGenerator.IndexFromContainer(targetContainer);

                if (targetStartX > draggedStartX && draggedDeltaEndX >= targetMidX)
                {
                    ((TranslateTransform) targetContainer.RenderTransform).X = -draggedBounds.Width;
                    _targetIndex = _targetIndex == -1 ? targetIndex : targetIndex > _targetIndex ? targetIndex : _targetIndex;
                    Debug.WriteLine($"Moved Right {_draggedIndex} -> {_targetIndex}");
                }
                else if (targetStartX < draggedStartX && draggedDeltaStartX <= targetMidX)
                {
                    ((TranslateTransform) targetContainer.RenderTransform).X = draggedBounds.Width;
                    _targetIndex = _targetIndex == -1 ? targetIndex : targetIndex < _targetIndex ? targetIndex : _targetIndex;
                    Debug.WriteLine($"Moved Left {_draggedIndex} -> {_targetIndex}");
                }
                else
                {
                    ((TranslateTransform) targetContainer.RenderTransform).X = 0;
                }

                i++;
            }

            Debug.WriteLine($"Moved {_draggedIndex} -> {_targetIndex}");
        }
    }
}