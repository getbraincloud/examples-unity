using TMPro;
using UnityEngine;

/// <summary>
/// A single floating damage number that pops off a unit/structure when it takes a hit,
/// arcs upward, fades, and destroys itself.
///
/// Built entirely in code (world-space TextMeshPro, no Canvas, no prefab) so BaseHealthBehavior
/// doesn't need a serialized reference wired onto every troop and structure prefab.
/// Spawned from BaseHealthBehavior.Damage(), which is the single choke point every damage
/// source (melee via TroopAI, projectiles via ProjectileMovement) already funnels through.
/// </summary>
public class FloatingDamageNumber : MonoBehaviour
{
    //Hits you land on the enemy read cyan (data); hits you take read red.
    public static readonly Color EnemyHitColor = new Color(0.40f, 0.95f, 1.00f);
    public static readonly Color SelfHitColor  = new Color(1.00f, 0.35f, 0.30f);

    private const float Lifetime = 0.85f;
    private const float RiseSpeed = 9f;
    private const float Gravity = 11f;

    private TextMeshPro _label;
    private Vector3 _velocity;
    private Camera _camera;
    private float _elapsed;
    private float _baseScale = 1f;

    /// <param name="worldPosition">Where the number appears (usually just above the target).</param>
    /// <param name="amount">Damage dealt.</param>
    /// <param name="color">Use EnemyHitColor / SelfHitColor.</param>
    /// <param name="scale">World scale of the text, sized from the target so big structures get big numbers.</param>
    public static void Spawn(Vector3 worldPosition, int amount, Color color, float scale)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = worldPosition;

        var label = go.AddComponent<TextMeshPro>();
        label.text = amount.ToString();
        label.color = color;
        label.fontSize = 14f;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        //Sit on top of the board and the bots rather than z-fighting into them.
        label.GetComponent<MeshRenderer>().sortingOrder = 500;

        var popup = go.AddComponent<FloatingDamageNumber>();
        popup._label = label;
        popup._baseScale = scale;
    }

    private void Start()
    {
        _camera = Camera.main;
        _velocity = new Vector3(Random.Range(-2f, 2f), RiseSpeed, 0f);
        transform.localScale = Vector3.one * _baseScale;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        transform.position += _velocity * Time.deltaTime;
        _velocity.y -= Gravity * Time.deltaTime;   //slight arc so numbers don't all rise identically

        //Billboard toward the camera so the number is readable from the fixed game angle.
        if (_camera != null)
        {
            transform.rotation = _camera.transform.rotation;
        }

        float t = _elapsed / Lifetime;

        //Quick pop out, then settle.
        float pop = t < 0.18f
            ? Mathf.Lerp(0.5f, 1.2f, t / 0.18f)
            : Mathf.Lerp(1.2f, 1f, (t - 0.18f) / 0.82f);
        transform.localScale = Vector3.one * (_baseScale * pop);

        //Hold opacity briefly, then fade out.
        if (_label != null)
        {
            Color c = _label.color;
            c.a = t < 0.55f ? 1f : Mathf.Clamp01(1f - (t - 0.55f) / 0.45f);
            _label.color = c;
        }

        if (_elapsed >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}
