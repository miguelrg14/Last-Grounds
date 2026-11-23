using UnityEngine;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>Pulses a text's alpha ("PRESS ANY KEY").</summary>
    [RequireComponent(typeof(Graphic))]
    public class UITextBlink : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float period = 1.2f;
        [SerializeField, Range(0f, 1f)] float minAlpha = 0.15f;

        Graphic graphic;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
        }

        void Update()
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / period);
            Color c = graphic.color;
            c.a = Mathf.Lerp(minAlpha, 1f, wave);
            graphic.color = c;
        }
    }
}
