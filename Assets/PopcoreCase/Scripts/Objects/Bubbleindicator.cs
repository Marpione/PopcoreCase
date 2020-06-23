using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Bubbleindicator : MonoBehaviour
{
    private Vector3 lastPos = Vector3.zero;

    private void Update()
    {
        if(transform.position != lastPos)
        {
            lastPos = transform.position;
            transform.DOScale(Vector3.zero, 0.2f).From().OnComplete(() => transform.localScale = Vector3.one);
        }
    }
}
