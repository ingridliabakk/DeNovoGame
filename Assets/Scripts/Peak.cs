using UnityEngine;

public class Peak : MonoBehaviour
{
    //public GameObject textObject;
    public GameObject line;

    public float intensity;
    internal int index;
    internal float coord;

    public void SetImageScale(float scale_x, float scale_y)
    {
        line.transform.localScale = new Vector3(scale_x, scale_y, 0);
    }

    public void SetPos(float pos_x, float pos_y)
    {
        Vector3 parent_transform = transform.parent.position;
        transform.localPosition = new Vector3((parent_transform.x + pos_x), (parent_transform.y + pos_y), 0);
    }
}
