using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class Draggable : MonoBehaviour//, IBeginDragHandler, IDragHandler, IEndDragHandler
{

/*     public void OnBeginDrag(PointerEventData eventData)
	{

		parentToReturnTo = this.transform.parent;
		this.transform.SetParent(this.transform.parent.parent);

		GetComponent<CanvasGroup>().blocksRaycasts = false;
	}

	public void OnDrag(PointerEventData eventData)
	{
		this.transform.position = eventData.position;
	}

	public void OnEndDrag(PointerEventData eventData)
	{

		this.transform.SetParent(parentToReturnTo);
		GetComponent<CanvasGroup>().blocksRaycasts = true;
	} */
}