using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProvidirectorGame
{
    public class FocusBarController : MonoBehaviour
    {
        public Image bar;
        public float value;
        public float maxValue;
        public GameObject maximumDisplay;
        public TextMeshProUGUI targetText;
        public TextMeshProUGUI cooldownText;

        void Update()
        {
            bar.fillAmount = value / maxValue;
            cooldownText.text = Math.Ceiling(value).ToString();
            if (value <= 0)
            {
                maximumDisplay?.SetActive(true);
                cooldownText.gameObject.SetActive(false);
            }
            else
            {
                maximumDisplay?.SetActive(false);
                cooldownText.gameObject.SetActive(true);
            }
        }
    }
}
