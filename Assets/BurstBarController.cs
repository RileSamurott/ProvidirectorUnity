using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProvidirectorGame
{
    public class BurstBarController : MonoBehaviour
    {
        public Image bar;
        public float value;
        public float maxValue;
        public Color baseColor;
        public Color pulseColor;
        public GameObject maximumDisplay;

        // Update is called once per frame
        void Start()
        {
            SetIsMax(false);
        }

        void Update()
        {
            bar.fillAmount = value / maxValue;
            if (value >= maxValue)
            {
                bar.color = pulseColor;
                maximumDisplay.SetActive(true);
            }
            else
            {
                bar.color = baseColor;
                maximumDisplay.SetActive(false);
            }
        }

        public void SetIsMax(bool value)
        {
            if (value)
            {
                maximumDisplay.SetActive(true);
                bar.color = pulseColor;
            }
            else
            {
                maximumDisplay.SetActive(false);
                bar.color = baseColor;
            }
        }

        public IEnumerator PulseUntilDepleted()
        {
            maximumDisplay.SetActive(false);
            while (value > 0)
            {
                if (bar.color == baseColor) bar.color = pulseColor;
                else bar.color = baseColor;
                yield return new WaitForSeconds(.05f);
            }
            bar.color = baseColor;
        }
    }
}
