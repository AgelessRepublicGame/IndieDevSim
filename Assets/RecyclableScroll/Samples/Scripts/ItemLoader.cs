using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace One.RecyclableScroll.Sample
{
    public class ItemLoader : MonoBehaviour
    {
        [SerializeField] private RecyclableScroll recyclableScroll;

        [SerializeField] private List<Item> items;

        private void Start()
        {
            recyclableScroll.Init(UpdateView);
            recyclableScroll.TryGetComponentsInContentChildren(ref items);

            UpdateView();
        }

        private void UpdateView()
        {
            int minIndex = recyclableScroll.MinIndex;
            int currentIndex;

            for (int i = 0; i < items.Count; i++)
            {
                currentIndex = minIndex + i;
                if (currentIndex >= 0 && currentIndex < recyclableScroll.TotalItems)
                {
                    items[i].SetNumber(currentIndex);
                }
                else
                {
                    items[i].Hide();
                }
            }
        }
    }
}
