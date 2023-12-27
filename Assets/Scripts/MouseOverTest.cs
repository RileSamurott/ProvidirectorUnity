using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverTest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Start is called before the first frame update
    void IPointerEnterHandler.OnPointerEnter(PointerEventData ed) {
        Debug.LogFormat("Entered {0}", gameObject.name);
    }
    void IPointerExitHandler.OnPointerExit(PointerEventData ed)
    {
        Debug.LogFormat("Exited {0}", gameObject.name);
    }
}
