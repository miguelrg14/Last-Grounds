using UnityEngine;

namespace LastGrounds
{
    /// <summary>Plays a looping frame-by-frame sprite animation (GameMaker-style image_speed).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0f)] float framesPerSecond = 8f;
        [SerializeField] bool randomStartFrame = true;

        SpriteRenderer spriteRenderer;
        float offset;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
            offset = randomStartFrame && frames.Length > 0 ? Random.Range(0, frames.Length) : 0f;
        }

        void Update()
        {
            if (frames.Length == 0 || framesPerSecond <= 0f) return;
            int index = (int)(Time.time * framesPerSecond + offset) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }
    }
}
