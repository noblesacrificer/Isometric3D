using System;
using System.Collections;
using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    [SerializeField] private Light pointLight;
    [SerializeField] private float min = 5f;
    [SerializeField] private float max = 8f;
    [SerializeField] private float delay = 0.1f;

    void Start()
    {
        StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            float safeDelay = Mathf.Max(delay, 0.01f); // Prevents delay from ever being 0 or negative
            float startIntensity = pointLight.intensity;
            float targetIntensity = UnityEngine.Random.Range(min, max);
            float elapsed = 0f;

            while (elapsed < safeDelay)
            {
                elapsed += Time.deltaTime;
                pointLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / safeDelay);
                yield return null;
            }

            pointLight.intensity = targetIntensity;
        }
    }

}
