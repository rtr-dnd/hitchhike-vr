using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manus.Hand;

public class ScaledHOMERController : MonoBehaviour
{
  public GameObject handWrap;
  public GameObject headOrigin;
  public GameObject tracker;
  public GameObject rayDirection;
  LineRenderer lineRenderer;

  int maxRaycastDistance = 100;
  Vector3? filteredPosition = null;
  float ratio = 0.3f;

  void Start()
  {
    lineRenderer = handWrap.GetComponent<LineRenderer>();
  }

  void Update()
  {
    handWrap.transform.position = tracker.transform.position;
    handWrap.transform.rotation = tracker.transform.rotation;
    lineRenderer.SetPosition(0, tracker.transform.position);
    if (!filteredPosition.HasValue)
    {
      filteredPosition = rayDirection.transform.forward;
    }
    else
    {
      filteredPosition = filteredPosition.Value * (1 - ratio) + (rayDirection.transform.forward * maxRaycastDistance) * ratio;
    }
    lineRenderer.SetPosition(1, filteredPosition.Value);
  }
}