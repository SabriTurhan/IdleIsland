using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UnlockableToolFloatingText : FloatingTextBaseBehavior
    {
        [SerializeField] Image iconImage;

        [Space]
        [SerializeField] Vector3 offset;
        [SerializeField] float time;
        [SerializeField] Ease.Type easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        private Vector3 defaultScale;

        private TweenCase scaleTween;
        private TweenCase moveTween;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public void Initialise(UnlockableTool unlockableTool)
        {
            if(unlockableTool != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = unlockableTool.Icon;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;
            textRef.color = color;

            transform.localScale = Vector3.zero;
            scaleTween = transform.DOScale(defaultScale * scaleMultiplier, scaleTime).SetCurveEasing(scaleAnimationCurve);
            moveTween = transform.DOMove(transform.position + offset, time).SetEasing(easing).OnComplete(delegate
            {
                gameObject.SetActive(false);

                InvokeCompleteEvent();
            });
        }

        public void SetText(string text)
        {
            textRef.text = text;
        }

        public void Reset()
        {
            scaleTween.KillActive();
            moveTween.KillActive();
        }
    }
}
