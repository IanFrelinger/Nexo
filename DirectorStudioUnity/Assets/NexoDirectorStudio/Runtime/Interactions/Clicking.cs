using UnityEngine;
using UnityEngine.EventSystems;

namespace NexoDirectorStudio.Interactions
{
    /// <summary>
    /// Utility for programmatic interaction simulation in headless environments.
    /// Simulates mouse clicks and pointer events for testing interactions.
    /// </summary>
    public static class Clicking
    {
        private static PointerEventData _pointerEventData;
        private static bool _initialized = false;
        
        /// <summary>
        /// Initialize the clicking system
        /// </summary>
        static void Initialize()
        {
            if (_initialized) return;
            
            _pointerEventData = new PointerEventData(EventSystem.current);
            _initialized = true;
            
            Debug.Log("✅ Clicking system initialized");
        }
        
        /// <summary>
        /// Simulate a click on a GameObject
        /// </summary>
        public static void Click(GameObject target)
        {
            if (target == null) return;
            
            Initialize();
            
            if (EventSystem.current == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found - cannot simulate click");
                return;
            }
            
            // Update pointer position to target's screen position
            var camera = Camera.main;
            if (camera != null)
            {
                _pointerEventData.position = camera.WorldToScreenPoint(target.transform.position);
            }
            else
            {
                _pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);
            }
            
            // Execute pointer events in sequence
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerUpHandler);
            
            Debug.Log($"🖱️ Simulated click on {target.name}");
        }
        
        /// <summary>
        /// Simulate a click on a specific position in screen space
        /// </summary>
        public static void ClickAt(Vector2 screenPosition)
        {
            Initialize();
            
            if (EventSystem.current == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found - cannot simulate click");
                return;
            }
            
            _pointerEventData.position = screenPosition;
            
            // Find object at position
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(_pointerEventData, results);
            
            if (results.Count > 0)
            {
                var target = results[0].gameObject;
                Click(target);
            }
            else
            {
                Debug.LogWarning($"⚠️ No object found at screen position {screenPosition}");
            }
        }
        
        /// <summary>
        /// Simulate a click on a world position
        /// </summary>
        public static void ClickAtWorld(Vector3 worldPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("⚠️ No MainCamera found - cannot convert world to screen position");
                return;
            }
            
            var screenPosition = camera.WorldToScreenPoint(worldPosition);
            ClickAt(screenPosition);
        }
        
        /// <summary>
        /// Simulate a hover over a GameObject
        /// </summary>
        public static void Hover(GameObject target)
        {
            if (target == null) return;
            
            Initialize();
            
            if (EventSystem.current == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found - cannot simulate hover");
                return;
            }
            
            var camera = Camera.main;
            if (camera != null)
            {
                _pointerEventData.position = camera.WorldToScreenPoint(target.transform.position);
            }
            
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerEnterHandler);
            
            Debug.Log($"🖱️ Simulated hover on {target.name}");
        }
        
        /// <summary>
        /// Simulate a drag operation
        /// </summary>
        public static void Drag(GameObject target, Vector2 startPosition, Vector2 endPosition)
        {
            if (target == null) return;
            
            Initialize();
            
            if (EventSystem.current == null)
            {
                Debug.LogWarning("⚠️ No EventSystem found - cannot simulate drag");
                return;
            }
            
            // Start drag
            _pointerEventData.position = startPosition;
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.beginDragHandler);
            
            // Drag to end position
            _pointerEventData.position = endPosition;
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.dragHandler);
            
            // End drag
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(target, _pointerEventData, ExecuteEvents.pointerUpHandler);
            
            Debug.Log($"🖱️ Simulated drag on {target.name} from {startPosition} to {endPosition}");
        }
    }
}
