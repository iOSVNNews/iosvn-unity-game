using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    public sealed class PixelSkillSpriteAnimator : MonoBehaviour
    {
        private Image target;
        private Sprite[] frames;
        private float framesPerSecond = 12f;
        private float phase;

        public void SetFrames(Sprite[] value, float rate, float offset)
        {
            target = GetComponent<Image>();
            frames = value;
            framesPerSecond = Mathf.Max(1f, rate);
            phase = offset;
            if (target != null && frames != null && frames.Length > 0) target.sprite = frames[0];
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            var index = Mathf.FloorToInt((Time.unscaledTime + phase) * framesPerSecond) % frames.Length;
            target.sprite = frames[index];
        }
    }
}
