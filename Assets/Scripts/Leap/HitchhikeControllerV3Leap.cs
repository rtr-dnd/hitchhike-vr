using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RootScript;
using ViveSR.anipal.Eye;
using Leap.Unity;
using Leap;

public class HitchhikeControllerV3Leap : MonoBehaviour
{
  public GameObject originalHandWrap;
  public GameObject originalHandArea;
  private List<GameObject> copiedHandWraps;
  public List<GameObject> copiedHandAreas;
  private List<int> handSwitchProgress;

  void Start()
  {
    copiedHandWraps = new List<GameObject>();
    handSwitchProgress = new List<int>();
    handSwitchProgress.Add(0);
    var originalHandGizmo = originalHandWrap.GetChildWithName("HandGizmo");
    foreach (var item in copiedHandAreas)
    {
      var initialHandPosition = item.GetChildWithName("InitialHandPosition");
      var tempHandWrap = GameObject.Instantiate(originalHandWrap);
      var displacement = tempHandWrap.GetChildWithName("displacement");
      displacement.transform.position = initialHandPosition.transform.position - originalHandGizmo.transform.position;
      displacement.transform.rotation = Quaternion.Inverse(initialHandPosition.transform.rotation) * originalHandGizmo.transform.rotation;
      var handGizmo = tempHandWrap.GetChildWithName("HandGizmo");
      handGizmo.transform.position += displacement.transform.position;
      handGizmo.transform.rotation *= displacement.transform.rotation;

      copiedHandWraps.Add(tempHandWrap);
      handSwitchProgress.Add(0);
    }

    foreach (var item in copiedHandWraps)
    {
      SetWrapEnabled(item, false);
    }
  }

  private void SetWrapEnabled(GameObject wrap, bool enabled)
  {
    wrap.GetChildWithName("LowPolyHands").SetActive(enabled);
    wrap.GetChildWithName("HandGizmo").SetActive(!enabled);
  }
}
