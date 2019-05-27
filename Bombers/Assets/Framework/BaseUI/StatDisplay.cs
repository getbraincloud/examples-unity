using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gameframework
{
    public class StatDisplay : BaseBehaviour
    {
        public Text StatNumber;
        public Image BarImage;

        public long CurrentValue { get; private set; }
        public long StatMax { get; private set; }
        public long StatMin { get; private set; }

        #region Public
        public void Init(long currentValue, long maxValue, long minValue = 0)
        {
            CurrentValue = currentValue;
            StatMax = maxValue;
            StatMin = minValue;
            SetValue(CurrentValue);
        }

        public Coroutine AnimateToWaitTillActive(long value, float time)
        {
            if (gameObject.activeInHierarchy)
                return StartCoroutine(Animate(value, time));
            else
            {
                return GStateManager.Instance.StartCoroutine(WaitForActive(value, time));
            }
        }

        public Coroutine AnimateTo(long value, float time)
        {
            if (gameObject.activeInHierarchy)
                return StartCoroutine(Animate(value, time));

            SetValue(value);
            CurrentValue = value;
            return null;
        }

        public void SetValue(float value)
        {
            long lValue = (long)value;
            if (BarImage) BarImage.fillAmount = Mathf.InverseLerp(StatMin, StatMax, value);
            if (StatNumber)
            {
                StatNumber.text = HudHelper.ToGUIString(lValue);
            }
        }
        #endregion

        #region Private
        private IEnumerator WaitForActive(long value, float time)
        {
            while (!gameObject.activeInHierarchy)
            {
                yield return YieldFactory.GetWaitForEndOfFrame();
            }
            yield return AnimateTo(value, time);
        }

        private IEnumerator Animate(long value, float time)
        {
            float startTime = Time.timeSinceLevelLoad;
            float startValue = CurrentValue;

            float percent = 0f;
            long curVal = 0;
            while (percent < 1f)
            {
                percent = (Time.timeSinceLevelLoad - startTime) / time;
                curVal = (long)Mathf.Lerp(startValue, value, percent);
                SetValue(curVal);
                yield return YieldFactory.GetWaitForEndOfFrame();
            }

            CurrentValue = curVal;
            yield return null;
        }
        #endregion
    }
}
