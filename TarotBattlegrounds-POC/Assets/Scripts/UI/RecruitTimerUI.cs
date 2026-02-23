using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace TarotBattlegrounds.UI
{
    /// <summary>
    /// T411: Animated recruit phase countdown timer.
    /// Displays remaining time with color transitions and pulse effects.
    /// </summary>
    public class RecruitTimerUI : MonoBehaviour
    {
        [Header("Timer Display")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image timerFill;
        [SerializeField] private Image timerBackground;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color warningColor = new Color(0.9f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color urgentColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        [Header("Thresholds")]
        [SerializeField] private float warningThreshold = 10f;
        [SerializeField] private float urgentThreshold = 5f;

        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinScale = 0.95f;
        [SerializeField] private float pulseMaxScale = 1.05f;

        private float totalTime;
        private float currentTime;
        private bool isUrgent;
        private RectTransform textRect;
        private Coroutine pulseCoroutine;

        private void Awake()
        {
            if (timerText != null)
                textRect = timerText.GetComponent<RectTransform>();
        }

        public void SetTotalTime(float total)
        {
            totalTime = total;
        }

        public void UpdateTimer(float remainingTime)
        {
            currentTime = remainingTime;

            // Update text
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
                timerText.text = $"{seconds}s";

                // Color transition
                if (remainingTime <= urgentThreshold)
                {
                    timerText.color = urgentColor;
                    if (!isUrgent) { isUrgent = true; if (pulseCoroutine != null) StopCoroutine(pulseCoroutine); pulseCoroutine = StartCoroutine(PulseTimer()); }
                }
                else if (remainingTime <= warningThreshold)
                {
                    timerText.color = warningColor;
                    isUrgent = false;
                }
                else
                {
                    timerText.color = normalColor;
                    isUrgent = false;
                }
            }

            // Update fill bar
            if (timerFill != null && totalTime > 0f)
            {
                float fill = Mathf.Clamp01(remainingTime / totalTime);
                timerFill.fillAmount = fill;
                timerFill.color = remainingTime <= urgentThreshold ? urgentColor :
                                  remainingTime <= warningThreshold ? warningColor : normalColor;
            }
        }

        private IEnumerator PulseTimer()
        {
            while (isUrgent && textRect != null)
            {
                float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
                float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, t);
                textRect.localScale = Vector3.one * scale;
                yield return null;
            }

            if (textRect != null)
                textRect.localScale = Vector3.one;
        }

        public void Hide()
        {
            isUrgent = false;
            if (timerText != null) timerText.text = "";
            if (timerFill != null) timerFill.fillAmount = 0f;
        }
    }
}
