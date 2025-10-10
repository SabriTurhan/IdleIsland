using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Watermelon
{
    public class HorizontalStorageCanvas : WorldSpaceCanvas
    {
        [SerializeField] GameObject resourceIndicatorPrefab;
        [SerializeField] Transform indicatorsContainer;

        [SerializeField] GameObject emptyStorageIndicator;
        [SerializeField] TMP_Text emptyStorageText;

        private List<SimpleResourceIndicator> indicators = new List<SimpleResourceIndicator>();

        public void SetData(ResourcesList data, int capacity)
        {
            Clear();

            for (int i = 0; i < data.Count; i++)
            {
                Resource resource = data[i];

                SimpleResourceIndicator indicator = Instantiate(resourceIndicatorPrefab).GetComponent<SimpleResourceIndicator>();

                indicator.transform.SetParent(indicatorsContainer);
                indicator.SetData(resource);

                indicators.Add(indicator);
            }

            if (data.IsNullOrEmpty())
            {
                emptyStorageIndicator.SetActive(true);
                emptyStorageText.text = $"0/{capacity}";
            }
            else
            {
                emptyStorageIndicator.SetActive(false);
            }
        }

        public void Clear()
        {
            foreach (var indicator in indicators)
            {
                indicator.Clear();
            }

            indicators.Clear();
        }
    }
}
