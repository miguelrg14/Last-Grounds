using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>
    /// Grows the highlighted button and shrinks it while pressed, like oBoton did with
    /// image_xscale/yscale. Hovering with the mouse selects the button, so mouse and
    /// keyboard always agree on which one is highlighted.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] float highlightScale = 1.1f;
        [SerializeField] float pressedScale = 0.95f;
        [SerializeField] float sharpness = 20f;
        [SerializeField] Color idleTint = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] Color highlightTint = Color.white;

        Graphic graphic;
        Vector3 baseScale;
        bool selected;
        bool pressed;

        void Awake()
        {
            baseScale = transform.localScale;
            graphic = GetComponent<Selectable>().targetGraphic;
        }

        void OnEnable()
        {
            EventSystem es = EventSystem.current;
            selected = es != null && es.currentSelectedGameObject == gameObject;
            pressed = false;
            transform.localScale = baseScale * (selected ? highlightScale : 1f);
            if (graphic != null) graphic.color = selected ? highlightTint : idleTint;
        }

        void Update()
        {
            float target = pressed ? pressedScale : (selected ? highlightScale : 1f);
            float t = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, t);
            if (graphic != null) graphic.color = Color.Lerp(graphic.color, selected ? highlightTint : idleTint, t);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem es = EventSystem.current;
            if (es != null && GetComponent<Selectable>().IsInteractable()) es.SetSelectedGameObject(gameObject);
        }

        public void OnPointerDown(PointerEventData eventData) { pressed = true; }
        public void OnPointerUp(PointerEventData eventData) { pressed = false; }
        public void OnSelect(BaseEventData eventData) { selected = true; }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            pressed = false;
        }
    }
}
