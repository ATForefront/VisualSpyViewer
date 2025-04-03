using TMPro;
using UnityEngine;

namespace SwitchControllerVisualizer.KeyInputLogger.Visualizer.Text
{
    [RequireComponent(typeof(TMP_Text))]
    public class KeyInputVisualText : KeyInputVisual
    {
        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }
        TMP_Text _text;
        public void SetText(string text)
        {
            _text.text = text;
        }
    }
}
