using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class DraggableBox : MonoBehaviour
{



    public void SetScale(float scale_x, float scale_y)
    {
           transform.localScale = new Vector3(scale_x, scale_y, 0);
    }

    public void SetPos(float pos_x, float pos_y)
    {
        Vector3 parent_transform = transform.parent.position;
        transform.localPosition = new Vector3((parent_transform.x + pos_x), (parent_transform.y + pos_y), 0);
    }

    
}