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
  public GameObject tracker;
  public GameObject rayDirection;
  LineRenderer lineRenderer;
  [SerializeField] Manus.Hand.Gesture.GestureBase grabGesture;
  bool isGrabbing;
  GameObject hoveredObject;
  GameObject selectedObject;

  // // homer stuff
  // Vector3 torsoCenter;
  // float D_hand; // initial distance between torso and hand
  // float D_object; // initial distance between torso and object
  // Vector3 V_offset; // offset vector

  // sclaed homer stuff
  Vector3 lastHandPos;
  Vector3 lastVirtualHandPos;
  float SC = 0.2f;
  float v_min = 0.05f;
  Vector3 torsoCenter;
  float D_hand; // initial distance between torso and hand
  float D_object; // initial distance between torso and object
  Vector3 V_offset; // offset vector

  int maxRaycastDistance = 100;
  Vector3? filteredForward = null;
  Vector3? filteredPosition = null;
  HandGrabInteraction interaction;
  float ratio = 0.3f;

  void Start()
  {
    lineRenderer = handWrap.GetComponent<LineRenderer>();
    interaction = GetHandGrabInteractionFromWrap(handWrap);
  }

  void Update()
  {
    var currentIsGrabbing = grabGesture.Evaluate(GetHandFromWrap(handWrap));
    if (currentIsGrabbing && (hoveredObject != null || selectedObject != null))
    {
      if (hoveredObject != null) // object color change etc.
      {
        selectedObject = hoveredObject;

        // homer initialize
        // torsoCenter = new Vector3(
        //   headOrigin.transform.position.x,
        //   tracker.transform.position.y,
        //   headOrigin.transform.position.z
        // );
        // D_hand = Vector3.Distance(tracker.transform.position, torsoCenter);
        // D_object = Vector3.Distance(selectedObject.transform.position, torsoCenter);
        // V_offset = selectedObject.transform.position - D_object * (tracker.transform.position - torsoCenter) / D_hand;

        // scaled homer initialize
        lastHandPos = tracker.transform.position;
        lastVirtualHandPos = selectedObject.transform.position;
        torsoCenter = new Vector3(
                  headOrigin.transform.position.x,
                  tracker.transform.position.y,
                  headOrigin.transform.position.z
                );
        D_hand = Vector3.Distance(tracker.transform.position, torsoCenter);
        D_object = Vector3.Distance(selectedObject.transform.position, torsoCenter);
        V_offset = selectedObject.transform.position - D_object * (tracker.transform.position - torsoCenter) / D_hand;

        OnHoverEnd(selectedObject);
        OnSelect(selectedObject);
        lineRenderer.enabled = false;
      }

      // change hand position by homer
      // var D_currhand = Vector3.Distance(tracker.transform.position, torsoCenter);
      // var D_virthand = D_currhand * D_object / D_hand;
      // handWrap.transform.position = D_virthand * (tracker.transform.position - torsoCenter) / D_currhand + V_offset;
      // handWrap.transform.rotation = tracker.transform.rotation;

      // change hand position by scaled homer
      var V_hand_move = tracker.transform.position - lastHandPos;
      var D_hand_in_scaled = V_hand_move.magnitude;
      var velocity = D_hand_in_scaled / Time.deltaTime;
      Debug.Log(velocity); // todo: adjust SC and v_min
      if (velocity < v_min)
      {
        handWrap.transform.position = lastVirtualHandPos;
        handWrap.transform.rotation = tracker.transform.rotation;
      }
      else
      {
        var SD_hand = Mathf.Min(velocity / SC, 1.2f) * D_hand_in_scaled;
        var SP_hand = SD_hand * (V_hand_move / V_hand_move.magnitude) + tracker.transform.position;
        var D_virthand = (SP_hand - torsoCenter).magnitude * D_object / D_hand;
        var D_currhand = Vector3.Distance(tracker.transform.position, torsoCenter);

        handWrap.transform.position = D_virthand * (tracker.transform.position - torsoCenter) / D_currhand + V_offset;
        handWrap.transform.rotation = tracker.transform.rotation;
      }

      // scaled homer update val
      lastHandPos = tracker.transform.position;
      lastVirtualHandPos = handWrap.transform.position;

      if (hoveredObject != null) // grab action
      {
        var grabbable = selectedObject.GetComponent<GrabbableObject>();
        if (grabbable == null) grabbable = selectedObject.GetComponentInParent<GrabbableObject>();
        if (grabbable != null) interaction.GrabGrabbable(grabbable);
        hoveredObject = null;
      }
    }
    else
    {
      if (selectedObject)
      {
        OnSelectEnd(selectedObject);
        selectedObject = null;
        // handToSelectedOffset = Vector3.zero;
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