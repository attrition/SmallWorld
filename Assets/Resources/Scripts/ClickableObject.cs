using UnityEngine;
using System.Collections;

public delegate void OnMouseDownHandler(GameObject obj);
public delegate void OnMouseEnterHandler(GameObject obj);
public delegate void OnMouseExitHandler(GameObject obj);

public class ClickableObject : MonoBehaviour
{
    public OnMouseDownHandler downHandler = null;
    public OnMouseEnterHandler enterHandler = null;
    public OnMouseExitHandler exitHandler = null;

    void OnMouseEnter()
    {
        if (enterHandler != null)
        {
            enterHandler(this.gameObject);
        }
    }

    void OnMouseExit()
    {
        if (exitHandler != null)
        {
            exitHandler(this.gameObject);
        }
    }

    void OnMouseDown()
    {
        if (downHandler != null)
        {
            downHandler(this.gameObject);
        }
    }
}
