using Nexo.Core.UI.Primitives.Components;
using UnityEditor;
using UnityEngine;

namespace Nexo.Core.UI.Unity.Frameworks.Unity;

public class PrimitivesDemoWindow : EditorWindow
{
    private string _text = "";

    [MenuItem("Nexo/UI Primitives Demo")] 
    public static void ShowWindow()
    {
        var window = GetWindow<PrimitivesDemoWindow>();
        window.titleContent = new GUIContent("Nexo UI Primitives");
        window.Show();
    }

    private void OnGUI()
    {
        // Card
        var card = new CardPrimitive
        {
            Title = "Nexo Primitives",
            Subtitle = "Unity Editor Demo",
            Footer = "Footer info",
            Padding = 16,
            Gap = 8
        };

        UnityCardRenderer.Render(card, () =>
        {
            // Input
            var input = new InputPrimitive
            {
                Label = "Enter Text",
                Placeholder = "Type here...",
                Value = _text
            };
            _text = UnityInputRenderer.Render(input, GUILayout.ExpandWidth(true));

            GUILayout.Space(8);

            // Button
            var button = new ButtonPrimitive
            {
                Text = "Click Me",
            };
            if (UnityButtonRenderer.Render(button, GUILayout.Height(40)))
            {
                Debug.Log($"Button clicked with text: {_text}");
            }
        });
    }
}


