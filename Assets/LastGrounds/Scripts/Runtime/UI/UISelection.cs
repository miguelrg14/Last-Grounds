using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastGrounds
{
    /// <summary>Helpers so menus can always be driven with keyboard or gamepad.</summary>
    public static class UISelection
    {
        /// <summary>
        /// Selects on the next frame, so the key press that opened a panel
        /// is not also treated as a click on its first button.
        /// </summary>
        public static void SelectNextFrame(MonoBehaviour host, Selectable target)
        {
            if (host == null || target == null) return;
            host.StartCoroutine(SelectRoutine(target));
        }

        static IEnumerator SelectRoutine(Selectable target)
        {
            yield return null;
            Select(target);
        }

        public static void Select(Selectable target)
        {
            EventSystem es = EventSystem.current;
            if (es == null || target == null || !target.gameObject.activeInHierarchy) return;
            es.SetSelectedGameObject(target.gameObject);
        }

        public static void Clear()
        {
            EventSystem es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);
        }

        /// <summary>If nothing is selected (e.g. after a mouse click on empty space), arrow keys reselect the fallback.</summary>
        public static void Ensure(Selectable fallback)
        {
            EventSystem es = EventSystem.current;
            if (es == null || fallback == null) return;
            GameObject current = es.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy) return;
            if (GameInput.NavigatePressed()) Select(fallback);
        }
    }
}
