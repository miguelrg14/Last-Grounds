using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Switches <see cref="GameInput.TouchActive"/> between touch and keyboard play, and
    /// applies mobile defaults (60 fps, screen never sleeps). Created automatically.
    /// </summary>
    public class InputModeWatcher : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
            GameInput.SetTouchActive(Application.isMobilePlatform);

            GameObject go = new GameObject("[InputModeWatcher]");
            DontDestroyOnLoad(go);
            go.AddComponent<InputModeWatcher>();
        }

        void Update()
        {
            if (GameInput.TouchBegan()) GameInput.SetTouchActive(true);
            else if (!Application.isMobilePlatform && GameInput.KeyboardUsed()) GameInput.SetTouchActive(false);
        }
    }
}
