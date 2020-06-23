using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointClickInput : Singleton<PointClickInput>
{
    public Vector2 GetMousePosition(Vector2 casterPosition)
    {
            var mousePos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var delta = mousePos - casterPosition;
            return delta;
    }
}
