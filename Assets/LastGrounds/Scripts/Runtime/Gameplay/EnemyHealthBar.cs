using UnityEngine;

namespace LastGrounds
{
    /// <summary>Tiny bar above a zombie (the original used draw_healthbar, red to lime).</summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] SpriteRenderer fill;
        [SerializeField, Min(0f)] float widthPixels = 8f;
        [SerializeField] Color lowColor = Color.red;
        [SerializeField] Color fullColor = new Color(0f, 1f, 0f, 1f);

        Enemy enemy;

        void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        void OnEnable()
        {
            enemy.HealthChanged += Refresh;
            Refresh(enemy);
        }

        void OnDisable()
        {
            enemy.HealthChanged -= Refresh;
        }

        void Refresh(Enemy e)
        {
            if (fill == null) return;
            float ratio = e.MaxHealth > 0f ? Mathf.Clamp01(e.Health / e.MaxHealth) : 0f;
            float width = widthPixels * ratio;
            Transform t = fill.transform;
            t.localScale = new Vector3(width, t.localScale.y, 1f);
            t.localPosition = new Vector3((width - widthPixels) * 0.5f / PlayField.PixelsPerUnit, t.localPosition.y, 0f);
            fill.color = Color.Lerp(lowColor, fullColor, ratio);
        }
    }
}
