
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WindStreakEffect : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.Rigidbody playerRigidbody;
    public UnityEngine.Animator playerAnim;

    [Header("Falling Detection")]
    public float minimumFallSpeed = 5f;
    public float maximumFallSpeed = 30f;

    [Header("Streak Settings")]
    public float minimumSpawnRate = 2f;
    public float maximumSpawnRate = 28f;
    public float minimumLength = 20f;
    public float maximumLength = 90f;
    public float streakWidth = 2f;

    [Header("Appearance")]
    [Range(0f, 1f)]
    public float opacity = 0.65f;

    public float minimumLifetime = 0.15f;
    public float maximumLifetime = 0.4f;

    private UnityEngine.RectTransform canvasRect;
    private UnityEngine.Sprite whiteSprite;
    private float spawnTimer;

    private class Streak
    {
        public UnityEngine.RectTransform rect;
        public UnityEngine.UI.Image image;
        public UnityEngine.Vector2 direction;
        public float lifetime;
        public float age;
        public float initialAlpha;
    }

    private List<Streak> activeStreaks =
        new List<Streak>();

    void Start()
    {
        UnityEngine.Canvas parentCanvas =
            GetComponentInParent<UnityEngine.Canvas>();

        canvasRect = parentCanvas.GetComponent<UnityEngine.RectTransform>();

        UnityEngine.Texture2D texture =
            new UnityEngine.Texture2D(1, 1);

        texture.SetPixel(0, 0, UnityEngine.Color.white);
        texture.Apply();

        whiteSprite = UnityEngine.Sprite.Create(
            texture,
            new UnityEngine.Rect(0, 0, 1, 1),
            new UnityEngine.Vector2(0.5f, 0.5f)
        );
    }

    void Update()
    {
        if (playerRigidbody == null) return;

        float fallingSpeed = UnityEngine.Mathf.Max(
            0f,
            -playerRigidbody.linearVelocity.y
        );

        float intensity = UnityEngine.Mathf.InverseLerp(
            minimumFallSpeed,
            maximumFallSpeed,
            fallingSpeed
        );

        float spawnRate = UnityEngine.Mathf.Lerp(
            minimumSpawnRate,
            maximumSpawnRate,
            intensity
        );

        if (fallingSpeed < minimumFallSpeed)
        {
            spawnRate = 0f;
            playerAnim.SetBool("Diving", false);
        }
        else
        {
            playerAnim.SetBool("Diving", true);
        }

        if (spawnRate > 0f)
        {
            spawnTimer += UnityEngine.Time.deltaTime;

            float interval = 1f / spawnRate;

            while (spawnTimer >= interval)
            {
                SpawnStreak(intensity);
                spawnTimer -= interval;
            }
        }
        else
        {
            spawnTimer = 0f;
        }

        UpdateStreaks();
    }

    void SpawnStreak(float intensity)
    {
        float angle = UnityEngine.Random.Range(0f, 360f);
        float radians = angle * UnityEngine.Mathf.Deg2Rad;

        UnityEngine.Vector2 direction =
            new UnityEngine.Vector2(
                UnityEngine.Mathf.Cos(radians),
                UnityEngine.Mathf.Sin(radians)
            );

        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        float x = direction.x * width * 0.43f;
        float y = direction.y * height * 0.43f;

        UnityEngine.GameObject streakObject =
            new UnityEngine.GameObject("WindStreak");

        streakObject.transform.SetParent(transform, false);

        UnityEngine.RectTransform rect =
            streakObject.AddComponent<UnityEngine.RectTransform>();

        UnityEngine.UI.Image image =
            streakObject.AddComponent<UnityEngine.UI.Image>();

        image.sprite = whiteSprite;
        image.raycastTarget = false;

        float length = UnityEngine.Random.Range(
            minimumLength,
            maximumLength
        );

        rect.sizeDelta = new UnityEngine.Vector2(
            streakWidth,
            length
        );

        rect.anchoredPosition =
            new UnityEngine.Vector2(x, y);

        rect.localRotation =
            UnityEngine.Quaternion.Euler(
                0f,
                0f,
                angle - 90f
            );

        float lifetime = UnityEngine.Random.Range(
            minimumLifetime,
            maximumLifetime
        );

        float alpha = opacity *
            UnityEngine.Mathf.Lerp(0.4f, 1f, intensity);

        image.color = new UnityEngine.Color(
            1f, 1f, 1f, alpha
        );

        activeStreaks.Add(new Streak
        {
            rect = rect,
            image = image,
            direction = direction,
            lifetime = lifetime,
            age = 0f,
            initialAlpha = alpha
        });
    }

    void UpdateStreaks()
    {
        for (int i = activeStreaks.Count - 1; i >= 0; i--)
        {
            Streak streak = activeStreaks[i];

            streak.age += UnityEngine.Time.deltaTime;

            float progress = streak.age / streak.lifetime;

            streak.rect.anchoredPosition +=
                streak.direction * (500f * UnityEngine.Time.deltaTime);

            UnityEngine.Color colour = streak.image.color;

            colour.a = streak.initialAlpha * (1f - progress);

            streak.image.color = colour;

            if (progress >= 1f)
            {
                UnityEngine.GameObject.Destroy(
                    streak.rect.gameObject
                );

                activeStreaks.RemoveAt(i);
            }
        }
    }
}
