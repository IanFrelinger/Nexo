using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Color processing and HSV conversion functionality
    /// </summary>
    public partial class DynamicMaterialGenerator : IDynamicMaterialGenerator
    {
        private Color AdjustHue(Color color, float hue)
        {
            // Simple hue adjustment
            var hsv = ColorToHSV(color);
            hsv.H = hue;
            return HSVToColor(hsv);
        }

        private HSVColor ColorToHSV(Color color)
        {
            // Convert RGB to HSV
            var max = Math.Max(color.r, Math.Max(color.g, color.b));
            var min = Math.Min(color.r, Math.Min(color.g, color.b));
            var delta = max - min;

            var h = 0f;
            if (delta != 0)
            {
                if (max == color.r)
                    h = ((color.g - color.b) / delta) % 6;
                else if (max == color.g)
                    h = (color.b - color.r) / delta + 2;
                else
                    h = (color.r - color.g) / delta + 4;
            }

            h *= 60;
            if (h < 0) h += 360;

            return new HSVColor
            {
                H = h,
                S = max == 0 ? 0 : delta / max,
                V = max
            };
        }

        private Color HSVToColor(HSVColor hsv)
        {
            // Convert HSV to RGB
            var c = hsv.V * hsv.S;
            var x = c * (1 - Math.Abs((hsv.H / 60) % 2 - 1));
            var m = hsv.V - c;

            float r, g, b;
            if (hsv.H < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hsv.H < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hsv.H < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hsv.H < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hsv.H < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new Color(r + m, g + m, b + m, 1f);
        }
    }
}
