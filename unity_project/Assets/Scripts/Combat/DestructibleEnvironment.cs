using System.Collections;
using UnityEngine;

namespace QuyCocBatHoang.Combat
{
    public enum ObstacleType
    {
        Tree,       // Cổ Mộc (Cây cổ thụ)
        Bamboo,     // Khóm Trúc
        Rock,       // Tảng Đá Linh Thạch
        Grass,      // Bụi Cỏ / Linh Thảo
        Urn         // Vò Gốm Cổ
    }

    /// <summary>
    /// DestructibleEnvironment: Cây cỏ tương tác chuẩn Quỷ Cốc Bát Hoang
    /// - Bình thường: Cỏ cây hơi di chuyển lay nhẹ theo gió
    /// - Khi người chơi hoặc quái đánh vào:
    ///   + Cây rung lắc, lá rụng lả tả, gãy đổ nát thành mảnh vụn
    ///   + Đá nứt vỡ tan tành và rơi ra Linh Thạch
    ///   + Cỏ bị chém đứt bay các phiến lá
    /// </summary>
    public class DestructibleEnvironment : MonoBehaviour
    {
        [Header("Obstacle Config")]
        public ObstacleType type = ObstacleType.Tree;
        public float maxHp = 50f;
        public float currentHp;

        [Header("Wind Sway (Cỏ cây hơi di chuyển)")]
        public float swaySpeed = 2.0f;
        public float swayAngle = 3.5f;

        [Header("Visual & Effects")]
        public SpriteRenderer spriteRenderer;
        public Sprite brokenSprite; // Gốc cây cụt hoặc mảnh đá vụn
        public ParticleSystem hitParticles;    // Lá rơi / dăm gỗ / đá vụn
        public ParticleSystem breakParticles;  // Nổ tung mảnh vỡ
        public GameObject spiritStoneDropPrefab; // Rơi Linh Thạch khi vỡ đá

        private bool isBroken = false;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float randomPhase;

        private void Start()
        {
            currentHp = maxHp;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            randomPhase = Random.Range(0f, Mathf.PI * 2f);
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (isBroken) return;

            // Cỏ cây hơi lay động nhẹ theo gió
            if (type == ObstacleType.Tree || type == ObstacleType.Bamboo || type == ObstacleType.Grass)
            {
                float angle = Mathf.Sin(Time.time * swaySpeed + randomPhase) * swayAngle;
                transform.rotation = initialRotation * Quaternion.Euler(0, 0, angle);
            }
        }

        /// <summary>
        /// Nhận sát thương khi bị kiếm khí, chém, hoặc quái húc vào
        /// </summary>
        public void TakeDamage(float damage, Vector2 hitDirection)
        {
            if (isBroken) return;

            currentHp -= damage;
            StartCoroutine(ShakeRoutine(0.2f, 0.15f));

            if (hitParticles != null)
            {
                hitParticles.Play();
            }

            if (currentHp <= 0)
            {
                BreakObstacle(hitDirection);
            }
        }

        private void BreakObstacle(Vector2 hitDirection)
        {
            isBroken = true;

            // Hiệu ứng nổ mảnh vỡ
            if (breakParticles != null)
            {
                breakParticles.transform.parent = null;
                breakParticles.Play();
                Destroy(breakParticles.gameObject, 2.5f);
            }

            // Đổi hình ảnh sang gốc cây cụt hoặc mảnh vỡ
            if (brokenSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = brokenSprite;
                transform.rotation = initialRotation;
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }

            // Tắt va chạm để người chơi và kiếm khí xuyên qua
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Rơi Linh Thạch nếu là tảng đá hoặc vò gốm
            if ((type == ObstacleType.Rock || type == ObstacleType.Urn) && spiritStoneDropPrefab != null)
            {
                Instantiate(spiritStoneDropPrefab, transform.position, Quaternion.identity);
            }
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                transform.position = initialPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = initialPosition;
        }
    }
}
