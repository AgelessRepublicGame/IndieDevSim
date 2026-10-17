using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace One.RecyclableScroll
{
    public class RecyclableScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [SerializeField] protected ScrollRect scrollRect;
        [SerializeField] protected RectTransform contentRT;

        protected HorizontalOrVerticalLayoutGroup layoutGroup;
        protected RectTransform[] items;

        [SerializeField] protected int pageIndex = 0;

        [SerializeField] protected int totalItems = 100;

        [SerializeField] protected int maxPageIndex = 9;

        [SerializeField] protected bool isDragging = false;

        protected Vector2 oldVelocity = Vector2.zero;

        protected bool isUpdated = false;

        protected float originalSize;

        protected PointerEventData eventData;

        protected Action onUpdatePageEvent;

        public int PageIndex => pageIndex;

        public int TotalItems => totalItems;

        public int ItemsPerPage => items.Length;

        public int MinIndex => ((pageIndex <= 0) ? 0 : (pageIndex - 1)) * items.Length;

        [ContextMenu("Init")]
        private void Init() => Init(null);

        public virtual void Init(int totalItems, Action onUpdatePage)
        {
            this.totalItems = totalItems;
            Init(onUpdatePage);
        }

        public virtual void Init(Action onUpdatePage)
        {
            onUpdatePageEvent = onUpdatePage;

            pageIndex = 0;

            oldVelocity = Vector2.zero;

            isUpdated = false;

            contentRT.TryGetComponent(out layoutGroup);

            List<RectTransform> itemsList = new List<RectTransform>();
            TryGetComponentsInContentChildren(ref itemsList);
            items = itemsList.ToArray();

            maxPageIndex = Mathf.CeilToInt(totalItems / items.Length);
        }

        protected virtual void UpdatePage()
        {
            Canvas.ForceUpdateCanvases();
            oldVelocity = scrollRect.velocity;

            isUpdated = true;

            if (isDragging)
            {
                scrollRect.OnBeginDrag(eventData);
            }

            onUpdatePageEvent?.Invoke();
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (isDragging)
            {
                this.eventData = eventData;
            }
        }

        public virtual void TryGetComponentsInContentChildren<T>(ref List<T> elements)
        {
            if (elements == null)
                elements = new List<T>();
            else
                elements.Clear();

            for (int i = 0; i < contentRT.childCount; i++)
            {
                if (contentRT.GetChild(i).TryGetComponent(out T e))
                {
                    elements.Add(e);
                }
            }
        }

        public virtual void TryGetComponentsInContentChildren<T>(ref T[] elements)
        {
            List<T> values = new List<T>();

            TryGetComponentsInContentChildren(ref values);

            elements = values.ToArray();
        }
    }
}
