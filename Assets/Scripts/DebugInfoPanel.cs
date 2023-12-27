using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugInfoPanel : MonoBehaviour
{
    // Start is called before the first frame update
    public TextMeshProUGUI debugPanel;

    // Update is called once per frame
    public void SetDebugInfo(string s) {
        debugPanel.text = s;
    }
}
