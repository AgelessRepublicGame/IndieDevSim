using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace One.RecyclableScroll
{
    public class RecyclableScrollHorizontal : RecyclableScroll
    {
        public bool IsNextPage => pageIndex < maxPageIndex && contentRT.localPosition.x < -originalSize;

        public bool IsPreviousPage => pageIndex > 0 && contentRT.localPosition.x > 0;

        private void Update()
        {
            if (isUpdated)
            {
                scrollRect.velocity = oldVelocity;
                isUpdated = false;
            }

            if (IsNextPage)
            {
                NextPage();
            }

            if (IsPreviousPage)
            {
                PreviousPage();
            }
        }

        private void NextPage()
        {
            if (pageIndex == 0)
                contentRT.localPosition = new Vector3(
                                -originalSize,
                                contentRT.localPosition.y,
                                contentRT.localPosition.z
                            );
            else
                contentRT.localPosition += Vector3.right * originalSize;

            pageIndex += 1;

            UpdatePage();
        }

        private void PreviousPage()
        {
            if (pageIndex == 1)
                contentRT.localPosition = new Vector3(
                                0,
                                contentRT.localPosition.y,
                                contentRT.localPosition.z
                            );
            else
                contentRT.localPosition -= Vector3.right * originalSize;

            pageIndex -= 1;

            UpdatePage();
        }

        public override void Init(Action onUpdatePage)
        {
            base.Init(onUpdatePage);

            float itemWidth = items[0].rect.width;
            float spacing = (layoutGroup != null) ? layoutGroup.spacing : 0;

            originalSize = items.Length * (itemWidth + spacing);

            int itemsToAdd = Mathf.CeilToInt(scrollRect.viewport.rect.width / (itemWidth + spacing));

            for (int i = 0; i < itemsToAdd; i++)
            {
                RectTransform clone = Instantiate(items[i % items.Length], contentRT);
                clone.SetAsLastSibling();
            }

            UpdatePage();
        }
    }
}
