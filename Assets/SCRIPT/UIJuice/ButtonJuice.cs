using UnityEngine;
using UnityEngine.EventSystems;

// Drop-in button feedback: press-down squash + springy release.
// Add next to any Button component. No wiring needed beyond (optional) a click sound.
public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Range(0.7f, 1f)] public float pressedScale = 0.92f;
    public float speed = 14f;

    public AudioSource audioSource; // optional
    public AudioClip clickSound;    // optional

    Vector3 baseScale;
    float target = 1f;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        target = 1f;
        transform.localScale = baseScale;
    }

    void Update()
    {
        if (baseScale.x == 0f) return; // authored at zero scale — nothing to animate

        float current = transform.localScale.x / baseScale.x;
        if (Mathf.Approximately(current, target))
            return; // settled — skip the per-frame lerp (dozens of buttons live at once)

        float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * speed);
        if (Mathf.Abs(next - target) < 0.001f)
            next = target;
        transform.localScale = baseScale * next;
    }

    public void OnPointerDown(PointerEventData e)
    {
        target = pressedScale;
        Haptics.Light(); // no-op until a haptics plugin lands; call site is placed

        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void OnPointerUp(PointerEventData e)   { target = 1f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
}
