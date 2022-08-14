using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manus.Hand;
using RootScript;

public class HitchhikeHeadController : MonoBehaviour
{
  public GameObject originalHandWrap;
  public List<GameObject> copiedHandWraps;
  public GameObject headTargetObject;
  public GameObject headOrigin;
  public GameObject tracker;
  public Material enabledMaterial;
  public Material disabledMaterial;
  private int maxRaycastDistance = 100;
  private float headDirectionGizmoDistance = 0.75f;
  private int activeHandIndex = 0; // 0: original, 1~: copied
  private Vector3 activeHandRelativePos = Vector3.zero;

  // Start is called before the first frame update
  void Start()
  {
    foreach (var item in copiedHandWraps)
    {
      SetWrapEnabled(item, false);
    }
  }

  // Update is called once per frame
  void Update()
  {
    var headRay = GetGazeRay();
    headTargetObject.transform.position = headOrigin.transform.position;
    headTargetObject.transform.position += headRay.direction * headDirectionGizmoDistance;

    int layerMask = LayerMask.GetMask("Hitchhike");
    if (Physics.Raycast(headRay, out var hit, maxRaycastDistance, layerMask))
    {
      var newIndex = GetNewHandIndex(hit);

      if (newIndex != activeHandIndex)
      {
        // switch hand operation
        if (newIndex == 0)
        {
          SetWrapEnabled(copiedHandWraps[activeHandIndex - 1], false);
          SetWrapEnabled(originalHandWrap, true);
        }
        else
        {
          if (activeHandIndex == 0)
          {
            SetWrapEnabled(originalHandWrap, false);
          }
          else
          {
            SetWrapEnabled(copiedHandWraps[activeHandIndex - 1], false);
          }
          SetWrapEnabled(copiedHandWraps[newIndex - 1], true);
        }
        activeHandIndex = newIndex;

        // set offset pos
        if (activeHandIndex == 0)
        {
          activeHandRelativePos = Vector3.zero;
        }
        else
        {
          activeHandRelativePos = copiedHandWraps[activeHandIndex - 1].transform.position - tracker.transform.position;
        }
      }
    }

    // update hand pos
    if (activeHandIndex == 0)
    {
      originalHandWrap.transform.position = tracker.transform.position;
      originalHandWrap.transform.rotation = tracker.transform.rotation;
    }
    else
    {
      copiedHandWraps[activeHandIndex - 1].transform.position = tracker.transform.position + activeHandRelativePos;
      copiedHandWraps[activeHandIndex - 1].transform.rotation = tracker.transform.rotation;
    }
  }

  private void SetWrapEnabled(GameObject wrap, bool enabled)
  {
    GetHandAnimatorFromWrap(wrap).isEnabled = enabled;
    var newMaterial = enabled ? enabledMaterial : disabledMaterial;
    GetRendererFromWrap(wrap).materials = new Material[2] { newMaterial, newMaterial };

  }

  private SkinnedMeshRenderer GetRendererFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetChildWithName("mesh_hand_r").GetComponent<SkinnedMeshRenderer>();
  }
  private HandAnimator GetHandAnimatorFromWrap(GameObject wrap)
  {
    return wrap.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetComponent<HandAnimator>();
  }

  private int GetNewHandIndex(RaycastHit hit)
  {
    var target = hit.collider.gameObject;

    var i = copiedHandWraps.FindIndex(e => (
      GameObject.ReferenceEquals(e, target.transform.parent.gameObject)
    ));
    return i + 1; // original: FindIndex returns -1 so index becomes 0
  }

  private Ray GetGazeRay()
  {
    // var eyeData = GetCombinedSingleEyeData();
    // var gazeDirection = eyeData.gaze_direction_normalized;
    // gazeDirection.x *= -1; // right hand coor to left hand coor

    // return new Ray(headOrigin.transform.position, headOrigin.transform.rotation * gazeDirection);
    float rotationGain = 1.4f;
    // float rotationGain = 1.0f;

    var rayTransform = headOrigin.transform;
    rayTransform.localEulerAngles = new Vector3(
      rayTransform.localEulerAngles.x < 90.0f
        ? rayTransform.localEulerAngles.x * rotationGain
        : rayTransform.localEulerAngles.x > 270.0f
          ? (rayTransform.localEulerAngles.x - 360.0f) * rotationGain + 360.0f
          : rayTransform.localEulerAngles.x,
      rayTransform.localEulerAngles.y,
      rayTransform.localEulerAngles.z
    );
    Debug.Log(rayTransform.localEulerAngles.x);
    return new Ray(rayTransform.position, rayTransform.forward);
  }

  // private SingleEyeData GetCombinedSingleEyeData()
  // {
  //   var eyeData = SRanipal_Eye_v2.GetVerboseData(out var verboseData);
  //   return verboseData.combined.eye_data;
  // }
}

