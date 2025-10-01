#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine;

namespace NexoDirectorStudio.Input
{
    /// <summary>
    /// Ensures input devices are available in headless environments for DirectorStudio.
    /// Adds test devices when running in batch mode or headless mode.
    /// </summary>
    public static class HeadlessInputDevices
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void EnsureInputDevices()
        {
            // Only add devices in headless/batch mode
            if (Application.isBatchMode || !Application.isEditor)
            {
                AddTestDevices();
            }
        }
        
        static void AddTestDevices()
        {
            // Add keyboard if not present
            if (Keyboard.current == null)
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                Debug.Log("✅ Added test keyboard device for headless mode");
            }
            
            // Add mouse if not present
            if (Mouse.current == null)
            {
                var mouse = InputSystem.AddDevice<Mouse>();
                Debug.Log("✅ Added test mouse device for headless mode");
            }
            
            // Add gamepad if not present (optional)
            if (Gamepad.current == null)
            {
                var gamepad = InputSystem.AddDevice<Gamepad>();
                Debug.Log("✅ Added test gamepad device for headless mode");
            }
            
            // Set input update mode for headless compatibility
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            
            Debug.Log("✅ Headless input devices configured");
        }
        
        /// <summary>
        /// Manually add test devices (useful for testing)
        /// </summary>
        public static void AddTestDevicesManually()
        {
            AddTestDevices();
        }
        
        /// <summary>
        /// Remove all test devices
        /// </summary>
        public static void RemoveTestDevices()
        {
            var devices = InputSystem.devices.ToArray();
            foreach (var device in devices)
            {
                if (device is Keyboard || device is Mouse || device is Gamepad)
                {
                    InputSystem.RemoveDevice(device);
                }
            }
            
            Debug.Log("✅ Test input devices removed");
        }
    }
}
#endif
