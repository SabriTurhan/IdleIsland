using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class PauseMusicItem : PauseItem
    {
        [SerializeField] Image imageRef;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private void Start()
        {
            isActive = AudioController.GetVolume(AudioType.Music) != 0;

            if (isActive)
                imageRef.sprite = activeSprite;
            else
                imageRef.sprite = disableSprite;
        }

        protected override void Click()
        {
            isActive = !isActive;

            if (isActive)
            {
                imageRef.sprite = activeSprite;

                AudioController.SetVolume(AudioType.Music, 1f);
            }
            else
            {
                imageRef.sprite = disableSprite;

                AudioController.SetVolume(AudioType.Music, 0f);
            }

            // Play button sound
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }
    }
}
