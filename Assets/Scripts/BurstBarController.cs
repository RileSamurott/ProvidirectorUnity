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
        public string rank;
        public float value;
        public float maxValue;
        public bool invert;
        public GameObject maximumDisplay;
        public GameObject maxFlashObject;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI chargeText;
        public Coroutine overlayHandler;

        void Update()
        {
            if (invert) bar.fillAmount = 1 - value / maxValue;
            else bar.fillAmount = value / maxValue;
            
            chargeText.text = string.Format("{0:P0}", value / maxValue);
            if (value >= maxValue)
            {
                chargeText.gameObject.SetActive(false);
                maximumDisplay?.SetActive(true);
                if (overlayHandler == null) overlayHandler = StartCoroutine(FlashCoroutine());
            }
            else
            {
                chargeText.gameObject.SetActive(true);
                rankText.text = "Rank " + rank;
                maximumDisplay?.SetActive(false);
                overlayHandler = null;
            }
        }

        void OnDestroy() {
            if (overlayHandler != null) StopCoroutine(overlayHandler);
        }

        IEnumerator FlashCoroutine() {
            float toggleFlashTimer = 0.333f;
            float toggleTextTimer = 2f;
            bool textstate = false;
            maxFlashObject.SetActive(true);
            rankText.text = "Ready";
            while (value >= maxValue) {
                toggleFlashTimer -= 0.05f;
                toggleTextTimer -= 0.05f;
                if (toggleTextTimer <= 0f) {
                    toggleTextTimer += 2f;
                    textstate = !textstate;
                    rankText.text = textstate ? "Rank " + rank : "Ready";
                }
                if (toggleFlashTimer <= 0f) {
                    toggleFlashTimer += 0.333f;
                    maxFlashObject.SetActive(!maxFlashObject.activeSelf);
                }
                yield return new WaitForSeconds(0.05f);
            }
            maxFlashObject.SetActive(false);
            rankText.text = "Rank " + rank;
        }
    }
}
