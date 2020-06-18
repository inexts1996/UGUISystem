using System;
using System.Collections.Generic;
using UnityEngine;

public class testMonobehavior : MonoBehaviour
{
   private void Start()
   {
      List<Component> comList = new List<Component>();
      gameObject.GetComponentsInParent(false, comList);

      for (int i = 0; i < comList.Count; i++)
      {
        Debug.Log($"parentName:{comList[i].name}"); 
      }
   }

   protected void OnBeforeTransformParentChanged()
   {
      Debug.Log("OnBeforeTransformParentChanged");
   }

   protected void OnCanvasGroupChanged()
   {
      Debug.Log("OnCanvasGroupChanged");
   }

   protected void OnCanvasHierarchyChanged()
   {
      Debug.Log("OnCanvasHierarchyChanged");
   }

   protected void OnDidApplyAnimationProperties()
   {
      Debug.Log("OnDidApplyAnimationProperties");
   }

   protected void OnRectTransformDimensionsChange()
   {
      Debug.Log("OnRectTransformDimensionsChange");
   }

   protected void OnTransformParentChanged()
   {
      Debug.Log("OnTransformParentChanged");
   }
}
