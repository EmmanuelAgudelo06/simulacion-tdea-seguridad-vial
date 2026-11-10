using UnityEngine;

/// Sonidos generados por código (no hace falta importar archivos de audio).
public class AudioTdeA : MonoBehaviour
{
    AudioSource src;
    AudioClip frenazo, golpe;
    const int SR = 44100;

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0;
        frenazo = CrearFrenazo();
        golpe = CrearGolpe();
    }

    public void Frenazo() { if (src) src.PlayOneShot(frenazo, 0.6f); }
    public void Golpe() { if (src) src.PlayOneShot(golpe, 1f); }

    static AudioClip CrearFrenazo()
    {
        float dur = 1.3f; int n = (int)(dur * SR);
        var d = new float[n]; var rnd = new System.Random(3); float y = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, k = i / (float)n;
            float env = Mathf.Min(1, t * 25f) * (1 - k) * (1 - k);
            float chirrido = Mathf.Sin(2 * Mathf.PI * (1750 + 90 * Mathf.Sin(2 * Mathf.PI * 23 * t)) * t);
            float x = (float)(rnd.NextDouble() * 2 - 1); y += 0.25f * (x - y);
            d[i] = (0.35f * chirrido + 0.5f * y) * env;
        }
        var c = AudioClip.Create("Frenazo", n, 1, SR, false); c.SetData(d, 0); return c;
    }

    static AudioClip CrearGolpe()
    {
        float dur = 0.45f; int n = (int)(dur * SR);
        var d = new float[n]; var rnd = new System.Random(9); float y = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float grave = Mathf.Sin(2 * Mathf.PI * (95 - 40 * t) * t) * Mathf.Exp(-t * 9f);
            float x = (float)(rnd.NextDouble() * 2 - 1); y += 0.35f * (x - y);
            d[i] = Mathf.Clamp(0.9f * grave + 0.7f * y * Mathf.Exp(-t * 28f), -1f, 1f);
        }
        var c = AudioClip.Create("Golpe", n, 1, SR, false); c.SetData(d, 0); return c;
    }
}
