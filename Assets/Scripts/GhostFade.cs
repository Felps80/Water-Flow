using UnityEngine;

public class GhostFade : MonoBehaviour
{
    public float fadeDuration = 0.3f;  // Tempo de fade até ficar invisível
    public float startAlpha = 0.8f;    // Alpha inicial do fantasma
    private SpriteRenderer sr;
    private float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = startAlpha;
            sr.color = c;
        }
    }

    void Update()
    {
        if (sr == null)
            return;
        timer += Time.deltaTime;
        // Calcula quanto o alpha deve diminuir
        float newAlpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
        Color c = sr.color;
        c.a = newAlpha;
        sr.color = c;

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}