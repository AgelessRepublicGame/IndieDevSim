using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace One.RecyclableScroll.Sample
{
    public class Item : MonoBehaviour
    {
        public Text numberText;

        public void SetNumber(int number)
        {
            numberText.text = number.ToString();
            gameObject.name = $"Item {number}";

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
