using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "BubbleData", menuName = "PopcoreCase/BubbleData", order = 1)]
public class BubbleData : ScriptableObject
{
    public string LevelID;
    public int LevelNumber;
    [ColorPalette]
    public Color LevelColor;
}
