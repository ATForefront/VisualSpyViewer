using System.Linq;
using UnityEngine;

namespace SwitchControllerVisualizer.KeyInputLogger.Visualizer.Text
{
    public class KeyInputLogTextVisualizer : KeyInputLogVisualizer
    {
        protected override void UpdateKeyInputVisuals(KeyInputVisual[] keyInputVisuals, KeyLogger.KeyCode[] keyCodeHistory)
        {
            for (var i = 0; keyCodeHistory.Length > i; i += 1)
            {
                if (keyInputVisuals[i] is not KeyInputVisualText vt) { continue; }

                if (keyCodeHistory[i] is KeyLogger.KeyCode.Unknown) { vt.SetText(""); }
                else
                {
                    vt.SetText(keyCodeHistory[i].ToString());
                }

            }
        }
    }
}
