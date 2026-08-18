using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Code-only win celebration: bursts colored squares from this RectTransform's
// position, with gravity, spin and fade. No prefabs or textures needed.
// Add to a UI object inside a Canvas (e.g. an empty child of the game panel),
// then call Burst() — GameManager calls it automatically on a win if wired.
public class ConfettiBurst : MonoBehaviour
{
    public int pieces = 42;
    public float force = 620f;
    public float gravity = 1400f;
    public float lifetime = 1.6f;
    public bool radial;
    public int secondaryPieces;
    public float secondaryDelay = 0.12f;
    public RectTransform popTarget;

    static readonly Color[] Palette =
    {
        new Color(0.25f, 0.85f, 1f),   // cyan
        new Color(0.91f, 0.36f, 1f),   // magenta
        new Color(1f, 0.78f, 0.34f),   // gold
        new Color(0.91f, 0.93f, 1f),   // near-white
    };

    class Piece
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float spin;
        public float age;
    }

    readonly List<Piece> live = new List<Piece>();
    Coroutine secondaryRoutine;
    Coroutine popRoutine;

    public void Burst()
    {
        Spawn(pieces, 1f);

        if (secondaryRoutine != null) StopCoroutine(secondaryRoutine);
        if (secondaryPieces > 0)
            secondaryRoutine = StartCoroutine(SecondaryBurst());

        if (popRoutine != null) StopCoroutine(popRoutine);
        if (popTarget != null)
            popRoutine = StartCoroutine(PopTarget());
    }

    void Spawn(int count, float forceScale)
    {
        var parent = (RectTransform)transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("confetti", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            float size = Random.Range(10f, 22f);
            rt.sizeDelta = new Vector2(size, size * Random.Range(0.5f, 1f));
            rt.anchoredPosition = radial
                ? Random.insideUnitCircle * 18f
                : Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = Palette[Random.Range(0, Palette.Length)];
            img.raycastTarget = false;

            float ang = (radial
                ? Random.Range(0f, 360f)
                : Random.Range(35f, 145f)) * Mathf.Deg2Rad;
            float pow = force * forceScale * Random.Range(0.5f, 1.15f);

            live.Add(new Piece
            {
                rt = rt,
                img = img,
                vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * pow,
                spin = Random.Range(-540f, 540f),
                age = 0f,
            });
        }
    }

    IEnumerator SecondaryBurst()
    {
        yield return new WaitForSecondsRealtime(secondaryDelay);
        Spawn(secondaryPieces, 0.72f);
        secondaryRoutine = null;
    }

    IEnumerator PopTarget()
    {
        Vector3 baseScale = popTarget.localScale;
        popTarget.localScale = baseScale * 0.76f;

        const float rise = 0.14f;
        const float settle = 0.20f;
        float elapsed = 0f;
        while (elapsed < rise)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rise);
            popTarget.localScale = Vector3.Lerp(
                baseScale * 0.76f, baseScale * 1.12f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settle)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / settle);
            popTarget.localScale = Vector3.Lerp(
                baseScale * 1.12f, baseScale, t);
            yield return null;
        }

        popTarget.localScale = baseScale;
        popRoutine = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        secondaryRoutine = null;
        popRoutine = null;

        for (int i = live.Count - 1; i >= 0; i--)
            if (live[i].rt != null)
                Destroy(live[i].rt.gameObject);
        live.Clear();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.age += dt;

            if (p.age >= lifetime || p.rt == null)
            {
                if (p.rt != null) Destroy(p.rt.gameObject);
                live.RemoveAt(i);
                continue;
            }

            p.vel += Vector2.down * gravity * dt;
            p.rt.anchoredPosition += p.vel * dt;
            p.rt.Rotate(0f, 0f, p.spin * dt);

            var c = p.img.color;
            c.a = 1f - Mathf.Pow(p.age / lifetime, 2f);
            p.img.color = c;
        }
    }
}
