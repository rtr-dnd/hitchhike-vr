using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Manus.Hand;
using RootScript;
using Manus;
using Manus.Interaction;
using ViveSR.anipal.Eye;
using System;

public enum HandTrackingMode
{
  Manus,
  Leap
}

public class HitchhikeControllerV3 : MonoBehaviour
{
  public HandTrackingMode mode;
  public GameObject leapOrigin;
  public HandWrap originalHandWrap;
  public GameObject originalHandArea;
  private List<HandWrap> copiedHandWraps;
  public List<GameObject> copiedHandAreas;
  public bool frozen = false;
  public bool hideOriginal = false;
  public int defaultCopiedWrap = 0;
  public GameObject headOrigin;
  public GameObject tracker;
  public Material enabledMaterial;
  public Material disabledMaterial;
  public GameObject focusDepth;
  public bool scaleHandCollider = false;
  private int maxRaycastDistance = 100;
  [SerializeField] float raycastDistanceAcceptThreshold = 0.2f;
  // private List<float> raycastDistanceAcceptThresholds;
  private int activeHandIndex = 0; // 0: original, 1~: copied
  // private List<int> handSwitchProgress;
  private int handSwitchProgressThreshold = 50;
  public bool scaleHandWithArea = false;
  public Action<HandWrap> onGrab;
  public Action<HandWrap> onRelease;
  public bool isGrabbing;
  public HandWrap activeHandWrap
  {
    get
    {
      return activeHandIndex == 0 ? originalHandWrap : copiedHandWraps[activeHandIndex - 1];
    }
  }

  // Start is called before the first frame update
  void Start()
  {
    originalHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold;
    copiedHandWraps = new List<HandWrap>();
    foreach (var item in copiedHandAreas)
    {
      var initialHandPosition = item.GetChildWithName("InitialHandPosition");
      HandWrap tempHandWrap = null;
      if (mode == HandTrackingMode.Manus)
      {
        tempHandWrap = ManusInit(item, initialHandPosition);
        copiedHandWraps.Add(tempHandWrap);
      }
      else if (mode == HandTrackingMode.Leap)
      {
        tempHandWrap = LeapInit(item, initialHandPosition);
        copiedHandWraps.Add(tempHandWrap);
      }

      if (scaleHandCollider)
      {
        var distance = Vector3.Distance(originalHandArea.transform.position, item.transform.position);
        tempHandWrap.colliderScale = distance * 0.03f + 1;
        tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold * Mathf.Pow(tempHandWrap.colliderScale, 2);
      }
      else
      {
        tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold;
      }
      tempHandWrap.SetProgress(0, handSwitchProgressThreshold);
    }
    originalHandWrap.SetProgress(0, handSwitchProgressThreshold);
    foreach (var item in copiedHandWraps)
    {
      item.SetEnabled(false);
      item.SetProgress(0, handSwitchProgressThreshold);
    }

    // if original is hidden
    if (hideOriginal)
    {
      originalHandWrap.SetIsHidden(true);
      originalHandWrap.SetEnabled(false);
      activeHandIndex = defaultCopiedWrap + 1;
      copiedHandWraps[defaultCopiedWrap].SetEnabled(true);
    }
  }

  HandWrap ManusInit(GameObject handArea, GameObject initialHandPosition)
  {
    var tempHandWrapObject = GameObject.Instantiate(originalHandWrap.gameObject, initialHandPosition.transform.position, initialHandPosition.transform.rotation);
    var tempHandWrap = tempHandWrapObject.GetComponent<HandWrap>();
    // if (scaleHandWithArea)
    // {
    //   tempHandWrapObject.transform.localScale = Vector3.one * (Mathf.Max(handArea.transform.localScale.x, Mathf.Max(handArea.transform.localScale.y, handArea.transform.localScale.z)));
    // tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold * Mathf.Max(handArea.transform.localScale.x, Mathf.Max(handArea.transform.localScale.y, handArea.transform.localScale.z));
    // }
    // else
    // {
    //   tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold;
    // }
    // tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold * tempHandWrap.colliderScale;
    return tempHandWrap;
  }

  HandWrap LeapInit(GameObject handArea, GameObject initialHandPosition) // translate handwrap and set gizmo to initialhandposition
  {
    var areaToLeapPos = leapOrigin.transform.position - originalHandArea.transform.position;
    var areaToLeapRot = Quaternion.Inverse(leapOrigin.transform.rotation) * originalHandArea.transform.rotation;

    var tempHandWrapObject = GameObject.Instantiate(originalHandWrap.gameObject);
    var tempHandWrap = tempHandWrapObject.GetComponent<HandWrap>();

    tempHandWrap.SetHandPosRot(handArea.transform.position + areaToLeapPos, handArea.transform.rotation * areaToLeapRot);
    tempHandWrap.SetHandGizmoPosRot(initialHandPosition.transform.position, initialHandPosition.transform.rotation);
    // var displacement = tempHandWrapObject.GetChildWithName("displacement");
    // var originalHandGizmo = originalHandWrap.gameObject.GetChildWithName("HandGizmo");
    // displacement.transform.position = initialHandPosition.transform.position - originalHandGizmo.transform.position;
    // displacement.transform.rotation = Quaternion.Inverse(initialHandPosition.transform.rotation) * originalHandGizmo.transform.rotation;
    // var handGizmo = tempHandWrapObject.GetChildWithName("HandGizmo");
    // handGizmo.transform.position += displacement.transform.position;
    // handGizmo.transform.rotation *= displacement.transform.rotation;

    // if (scaleHandWithArea) // todo: leap scaling
    // {
    //   tempHandWrap.transform.localScale = Vector3.one * (Mathf.Max(handArea.transform.localScale.x, Mathf.Max(handArea.transform.localScale.y, handArea.transform.localScale.z)));
    //   raycastDistanceAcceptThresholds.Add(raycastDistanceAcceptThreshold * Mathf.Max(handArea.transform.localScale.x, Mathf.Max(handArea.transform.localScale.y, handArea.transform.localScale.z)));
    //   raycastDistanceAcceptThresholds.Add(raycastDistanceAcceptThreshold * Mathf.Max(handArea.transform.localScale.x, Mathf.Max(handArea.transform.localScale.y, handArea.transform.localScale.z)));
    // }
    // else
    // {
    // tempHandWrap.raycastDistanceAcceptThreshold = raycastDistanceAcceptThreshold;
    // }
    return tempHandWrap;
  }


  // Update is called once per frame
  void Update()
  {
    if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
    {
      Debug.Log("Eye tracking malfunctioning");
      return;
    }

    // hand position & switching update
    if (!frozen)
    {
      int layerMask = 1 << LayerMask.NameToLayer("Hitchhike");
      var gazeRay = GetGazeRay();
      var focusDistance = GetFocusDistance(layerMask) * 1.1f; // magic number
      if (focusDepth != null) focusDepth.transform.position = headOrigin.transform.position + gazeRay.direction * focusDistance;

      RaycastHit closestHit = new RaycastHit();
      float closestDistance = float.PositiveInfinity;
      foreach (var hit in Physics.RaycastAll(gazeRay, Mathf.Min(maxRaycastDistance, focusDistance), layerMask))
      {
        var wrap = GetHandWrapFromHit(hit);
        var i = GetHandWrapIndex(wrap);

        // finding a hit that's closest to focus point
        var colliderDistance = Vector3.Distance(hit.collider.gameObject.transform.position, headOrigin.transform.position);
        // var colliderDistance = hit.distance;
        if (Mathf.Abs(colliderDistance - focusDistance) > wrap.raycastDistanceAcceptThreshold) continue;
        if (Mathf.Abs(colliderDistance - focusDistance) < Mathf.Abs(closestDistance - focusDistance))
        {
          closestHit = hit;
          closestDistance = colliderDistance;
        }
      }

      var currentGazeIndex = -1;
      HandWrap currentGazeWrap = null;
      if (closestDistance < float.PositiveInfinity)
      {
        currentGazeWrap = GetHandWrapFromHit(closestHit);
        currentGazeIndex = GetHandWrapIndex(currentGazeWrap);
      }

      // update hand status
      for (var i = 0; i < 1 + copiedHandWraps.Count; i++)
      {
        if (i == currentGazeIndex && currentGazeIndex != activeHandIndex)
        {
          currentGazeWrap.SetProgress(currentGazeWrap.GetProgress() + 3, handSwitchProgressThreshold);
        }
        else
        {
          var thisWrap = i == 0 ? originalHandWrap : copiedHandWraps[i - 1];
          if (thisWrap.GetProgress() > 0) thisWrap.SetProgress(thisWrap.GetProgress() - 1, handSwitchProgressThreshold);
        }
      }
      // Debug.Log(string.Join(", ", handSwitchProgress.Select(i => i.ToString())));

      // switch hand operation
      for (var i = 0; i < 1 + copiedHandWraps.Count; i++)
      {
        var thisWrap = i == 0 ? originalHandWrap : copiedHandWraps[i - 1];
        if (thisWrap.GetProgress() < handSwitchProgressThreshold) continue;
        if (i == activeHandIndex) continue;

        thisWrap.SetProgress(0, handSwitchProgressThreshold);
        if (i == 0)
        {
          SwitchWrap(copiedHandWraps[activeHandIndex - 1], originalHandWrap, copiedHandAreas[activeHandIndex - 1]);
        }
        else
        {
          if (activeHandIndex == 0)
          {
            SwitchWrap(originalHandWrap, copiedHandWraps[i - 1], originalHandArea);
          }
          else
          {
            SwitchWrap(copiedHandWraps[activeHandIndex - 1], copiedHandWraps[i - 1], copiedHandAreas[activeHandIndex - 1]);
          }
        }
        activeHandIndex = i;
      }
    }

    // update hand pos
    if (activeHandIndex == 0)
    {
      if (mode == HandTrackingMode.Manus)
      {
        originalHandWrap.transform.position = tracker.transform.position;
        originalHandWrap.transform.rotation = tracker.transform.rotation;
      }
    }
    else
    {
      if (mode == HandTrackingMode.Manus) // leap stuff is written in DisplaceLeapHand.cs
      {
        var originalSpaceOrigin = originalHandArea.transform;
        var copiedSpaceOrigin = copiedHandAreas[activeHandIndex - 1].transform;

        var originalToCopiedRot = Quaternion.Inverse(copiedSpaceOrigin.rotation) * originalSpaceOrigin.rotation;
        var originalToCopiedScale = new Vector3(
          copiedSpaceOrigin.lossyScale.x / originalSpaceOrigin.lossyScale.x,
          copiedSpaceOrigin.lossyScale.y / originalSpaceOrigin.lossyScale.y,
          copiedSpaceOrigin.lossyScale.z / originalSpaceOrigin.lossyScale.z
        );

        var oMt = Matrix4x4.TRS(
          tracker.transform.position,
          tracker.transform.rotation,
          new Vector3(1, 1, 1)
        );

        var resMat =
        Matrix4x4.Translate(copiedSpaceOrigin.position - originalSpaceOrigin.position) // orignal to copied translation
        * Matrix4x4.TRS(
            originalSpaceOrigin.position,
            Quaternion.Inverse(originalToCopiedRot),
            originalToCopiedScale
        ) // translation back to original space and rotation & scale around original space
        * Matrix4x4.Translate(-originalSpaceOrigin.position) // offset translation for next step
        * oMt; // tracker

        copiedHandWraps[activeHandIndex - 1].transform.position = resMat.GetColumn(3);
        copiedHandWraps[activeHandIndex - 1].transform.rotation = resMat.rotation;
      }
    }

    // invoke actions
    var activeHandWrap = activeHandIndex == 0 ? originalHandWrap : copiedHandWraps[activeHandIndex - 1];
    var newGrabbing = activeHandWrap.GetManusHandGrabInteraction().grabbedObject != null;
    if (isGrabbing != newGrabbing)
    {
      if (newGrabbing)
      {
        if (onGrab != null) onGrab.Invoke(activeHandWrap);
      }
      else
      {
        if (onRelease != null) onRelease.Invoke(activeHandWrap);
      }
    }
    isGrabbing = newGrabbing;
  }

  private void SwitchWrap(HandWrap beforeWrap, HandWrap afterWrap, GameObject beforeArea)
  {
    if (mode == HandTrackingMode.Manus) // swap grabbing object
    {
      var interactionBefore = beforeWrap.GetManusHandGrabInteraction();
      var interactionAfter = afterWrap.GetManusHandGrabInteraction();
      var grabbedObject = interactionBefore.grabbedObject;
      if (grabbedObject != null)
      {
        var oldInfo = grabbedObject.hands[0];
        var grabbableObject = grabbedObject.gameObject.GetComponent<GrabbableObject>();

        interactionAfter.GrabGrabbable(grabbableObject);
        interactionBefore.Release();

        // overwrite grabbing info
        interactionAfter.grabbedObject.hands[0].distance = oldInfo.distance;
        interactionAfter.grabbedObject.hands[0].nearestColliderPoint = oldInfo.nearestColliderPoint;
        interactionAfter.grabbedObject.hands[0].handToObject = oldInfo.handToObject;
        interactionAfter.grabbedObject.hands[0].objectToHand = oldInfo.objectToHand;
        interactionAfter.grabbedObject.hands[0].objectInteractorForward = oldInfo.objectInteractorForward;
        interactionAfter.grabbedObject.hands[0].handToObjectRotation = oldInfo.handToObjectRotation;
      }
    }

    beforeWrap.SetEnabled(false);
    afterWrap.SetEnabled(true);

    // set beforewrap position back to original position
    var initialHandPosition = beforeArea.GetChildWithName("InitialHandPosition");
    beforeWrap.SetHandPosRot(initialHandPosition.transform.position, initialHandPosition.transform.rotation);
  }

  // private void SetWrapEnabled(GameObject wrap, bool enabled)
  // {
  //   if (mode == HandTrackingMode.Manus)
  //   {
  //     GetManusHandAnimatorFromWrap(wrap).isEnabled = enabled;
  //     var newMaterial = enabled ? enabledMaterial : disabledMaterial;
  //     GetManusRendererFromWrap(wrap).materials = new Material[2] { newMaterial, newMaterial };
  //   }
  //   else if (mode == HandTrackingMode.Leap)
  //   {
  //     wrap.GetChildWithName("LowPolyHands").SetActive(enabled);
  //     wrap.GetChildWithName("HandGizmo").SetActive(!enabled);
  //   }
  // }
  private SkinnedMeshRenderer GetManusRendererFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetChildWithName("mesh_hand_r").GetComponent<SkinnedMeshRenderer>();
  }
  private HandGrabInteraction GetManusHandGrabInteractionFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("Interaction").GetComponent<HandGrabInteraction>();
  }
  private HandAnimator GetManusHandAnimatorFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetComponent<HandAnimator>();
  }
  // private Canvas GetCanvasFromWrap(GameObject wrap)
  // {
  //   if (mode == HandTrackingMode.Manus) return wrap.GetChildWithName("Canvas").GetComponent<Canvas>();
  //   if (mode == HandTrackingMode.Leap) return wrap.GetChildWithName("HandGizmo").GetChildWithName("Canvas").GetComponent<Canvas>();
  //   return null;
  // }
  // private Image GetImageFromWrap(GameObject wrap)
  // {
  //   return GetCanvasFromWrap(wrap).gameObject.GetChildWithName("ProgressIndicator").GetChildWithName("Fill").GetComponent<Image>();
  // }


  // private void SetWrapCanvasEnabled(GameObject wrap, bool enabled)
  // {
  //   var canvas = GetCanvasFromWrap(wrap);
  //   canvas.gameObject.SetActive(enabled);
  // }
  // private void SetProgressOfWrap(GameObject wrap, float progress)
  // {
  //   if (progress <= 0.0f || progress >= 1.0f)
  //   {
  //     SetWrapCanvasEnabled(wrap, false);
  //   }
  //   else
  //   {
  //     SetWrapCanvasEnabled(wrap, true);
  //   }
  //   Image image = GetImageFromWrap(wrap);
  //   image.fillAmount = progress;
  // }
  // private int GetWrapIndexFromHit(RaycastHit hit)
  // {
  //   var target = hit.collider.gameObject;

  //   var i = copiedHandWraps.FindIndex(e => (
  //     GameObject.ReferenceEquals(e, target.transform.parent.gameObject)
  //   ));
  //   return i + 1; // original: FindIndex returns -1 so index becomes 0
  // }
  private HandWrap GetHandWrapFromHit(RaycastHit hit)
  {
    var target = hit.collider.gameObject;
    return target.GetComponentInParent<HandWrap>();
  }
  private int GetHandWrapIndex(HandWrap wrap)
  {
    var i = copiedHandWraps.FindIndex(e => e == wrap);
    return i + 1; // original: FindIndex returns -1 so index becomes 0
  }

  // eye tracking
  Vector3? filteredDirection = null;
  Vector3? filteredPosition = null;
  float? filteredDistance = null;
  float ratio = 0.3f;
  private Ray GetGazeRay()
  {
    var eyeData = GetCombinedSingleEyeData();
    var gazeDirection = eyeData.gaze_direction_normalized;
    gazeDirection.x *= -1; // right hand coor to left hand coor

    if (!filteredDirection.HasValue)
    {
      filteredDirection = headOrigin.transform.rotation * gazeDirection;
      filteredPosition = headOrigin.transform.position;
    }
    else
    {
      filteredDirection = filteredDirection.Value * (1 - ratio) + headOrigin.transform.rotation * gazeDirection * ratio;
      filteredPosition = filteredPosition.Value * (1 - ratio) + headOrigin.transform.position * ratio;
    }
    return new Ray(filteredPosition.Value, filteredDirection.Value);
  }

  private SingleEyeData GetCombinedSingleEyeData()
  {
    var eyeData = SRanipal_Eye_v2.GetVerboseData(out var verboseData);
    return verboseData.combined.eye_data;
  }

  private float GetFocusDistance(int layerMask)
  {
    var wasSuccess = SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out var combineRay, out var combineFocus, 0, float.MaxValue, layerMask);
    if (!filteredDistance.HasValue)
    {
      filteredDistance = combineFocus.distance;
    }
    else
    {
      filteredDistance = filteredDistance.Value * (1 - ratio) + combineFocus.distance * ratio;
    }
    return filteredDistance.Value;
  }
}
