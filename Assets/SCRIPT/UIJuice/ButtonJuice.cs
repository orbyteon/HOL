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
        float current = transform.localScale.x / baseScale.x;
        float next = Mathf.Lerp(current, target, Time.unscaledDeltaTime * speed);
        transform.localScale = baseScale * next;
    }

    public void OnPointerDown(PointerEventData e)
    {
        target = pressedScale;

        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void OnPointerUp(PointerEventData e)   { target = 1f; }
    public void OnPointerExit(PointerEventData e) { target = 1f; }
}
