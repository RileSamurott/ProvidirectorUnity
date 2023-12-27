using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProvidirectorGame
{
    public class MoneyBarController : MonoBehaviour
    {
        public Image bar;
        public TextMeshProUGUI[] valueTexts;
        public float value;
        public float maxValue;
        private Color _baseColor;
        public Color baseColor
        {
            get { return _baseColor; }
            set
            {
                _baseColor = value;
                if (bar) bar.color = baseColor;
            }
        }

        // Update is called once per frame
        void Start()
        {
            baseColor = bar.color;
        }

        void Update()
        {
            bar.fillAmount = value / maxValue;
            foreach (TextMeshProUGUI text in valueTexts)
            {
                text.text = String.Format("{0} / {1}", (int)value, (int)maxValue);
            }
        }

        public IEnumerator FlashBar()
        {
            bool flash = false;
            for (int i = 0; i < 10; i++)
            {
                if (flash) bar.color = Color.white;
                else bar.color = baseColor;
                flash = !flash;
                yield return new WaitForSeconds(.05f);
            }
            bar.color = baseColor;
        }
    }
}
