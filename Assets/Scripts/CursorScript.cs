using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorScript : MonoBehaviour
{
    public Texture2D cursorTexture;
    CursorMode mode = CursorMode.Auto;
    public Vector2 hotSpot;

    private void OnMouseEnter()
    {
        Cursor.SetCursor(cursorTexture, hotSpot, mode);
    }

    private void OnMouseExit()
    {
        Cursor.SetCursor(null, Vector2.zero, mode);
    }
}
