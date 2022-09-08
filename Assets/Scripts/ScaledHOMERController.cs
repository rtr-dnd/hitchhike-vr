using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manus.Hand;
using Manus.Interaction;
using RootScript;

public class ScaledHOMERController : MonoBehaviour
{
  public GameObject handWrap;
  public GameObject headOrigin;
  Vector3 fixedHeadOrigin;
  public GameObject tracker;
  public GameObject rayDirection;
  LineRenderer lineRenderer;
  [SerializeField] Manus.Hand.Gesture.GestureBase grabGesture;
  bool isGrabbing;
  GameObject hoveredObject;
  GameObject selectedObject;
  Vector3 handToSelectedOffset;

  int maxRaycastDistance = 100;
  Vector3? filteredForward = null;
  Vector3? filteredPosition = null;
  HandGrabInteraction interaction;
  float ratio = 0.3f;

  void Start()
  {
    lineRenderer = handWrap.GetComponent<LineRenderer>();
    interaction = GetHandGrabInteractionFromWrap(handWrap);
    fixedHeadOrigin = headOrigin.transform.position;
  }

  void Update()
  {
    var currentIsGrabbing = grabGesture.Evaluate(GetHandFromWrap(handWrap));
    if (currentIsGrabbing && (hoveredObject != null || selectedObject != null))
    {
      if (hoveredObject != null) // object color change etc.
      {
        selectedObject = hoveredObject;
        handToSelectedOffset = selectedObject.transform.position - tracker.transform.position;
        OnHoverEnd(selectedObject);
        OnSelect(selectedObject);
        lineRenderer.enabled = false;
      }

      // change hand position
      handWrap.transform.position = tracker.transform.position + handToSelectedOffset;
      handWrap.transform.rotation = tracker.transform.rotation;

      if (hoveredObject != null) // grab action
      {
        var grabbable = selectedObject.GetComponent<GrabbableObject>();
        interaction.GrabGrabbable(grabbable);
        hoveredObject = null;
      }
    }
    else
    {
      if (selectedObject)
      {
        OnSelectEnd(selectedObject);
        selectedObject = null;
        handToSelectedOffset = Vector3.zero;
        interaction.Release();
        lineRenderer.enabled = true;
      }

      handWrap.transform.position = tracker.transform.position;
      handWrap.transform.rotation = tracker.transform.rotation;
      if (!filteredForward.HasValue)
      {
        filteredForward = rayDirection.transform.forward;
        filteredPosition = rayDirection.transform.forward * maxRaycastDistance;
      }
      else
      {
        filteredForward = filteredForward.Value * (1 - ratio) + (rayDirection.transform.forward) * ratio;
        filteredPosition = filteredPosition.Value * (1 - ratio) + (rayDirection.transform.forward * maxRaycastDistance) * ratio;
      }

      lineRenderer.SetPosition(0, tracker.transform.position + filteredForward.Value * 0.2f);
      if (Physics.Raycast(tracker.transform.position + filteredForward.Value * 0.2f, filteredForward.Value, out var hit, maxRaycastDistance))
      {
        if (hit.collider.gameObject.GetComponent<GrabbableObject>() != null || hit.collider.gameObject.GetComponentInParent<GrabbableObject>() != null)
        {
          if (hit.collider.gameObject != hoveredObject)
          {
            if (hoveredObject != null) OnHoverEnd(hoveredObject);
            hoveredObject = hit.collider.gameObject;
            OnHover(hoveredObject);
          }
        }
        else
        {
          if (hoveredObject != null) OnHoverEnd(hoveredObject);
          hoveredObject = null;
        }
        lineRenderer.SetPosition(1, hit.point);
      }
      else
      {
        if (hoveredObject != null) OnHoverEnd(hoveredObject);
        hoveredObject = null;
        lineRenderer.SetPosition(1, filteredPosition.Value);
      }
    }
  }

  void OnHover(GameObject obj)
  {
    obj.GetComponent<MeshRenderer>().material.color = Color.yellow;
  }

  void OnHoverEnd(GameObject obj)
  {
    obj.GetComponent<MeshRenderer>().material.color = Color.white;
  }

  void OnSelect(GameObject obj)
  {
    obj.GetComponent<MeshRenderer>().material.color = Color.red;
  }

  void OnSelectEnd(GameObject obj)
  {
    obj.GetComponent<MeshRenderer>().material.color = Color.white;
  }


  private Hand GetHandFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetComponent<Hand>();
  }
  private HandGrabInteraction GetHandGrabInteractionFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("Interaction").GetComponent<HandGrabInteraction>();
  }
}