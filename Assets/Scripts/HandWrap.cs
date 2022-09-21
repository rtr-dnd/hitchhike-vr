using UnityEngine;
using UnityEngine.UI;
using RootScript;
using Manus.Interaction;
using Manus.Hand;

public class HandWrap : MonoBehaviour
{
  public HandTrackingMode device;
  public float raycastDistanceAcceptThreshold;
  private int handSwitchProgress;
  private bool isEnabled;
  public Material enabledMaterial;
  public Material disabledMaterial;
  public float colliderScale = 1;

  private void Start()
  {
    gameObject.GetChildWithName("Capsule").transform.localScale *= colliderScale;
  }

  public bool GetEnabled() { return isEnabled; }
  public void SetEnabled(bool enabled)
  {
    if (device == HandTrackingMode.Manus)
    {
      GetManusHandAnimator().isEnabled = enabled;
      var newMaterial = enabled ? enabledMaterial : disabledMaterial;
      GetManusRenderer().materials = new Material[2] { newMaterial, newMaterial };
    }
    else if (device == HandTrackingMode.Leap)
    {
      gameObject.GetChildWithName("LowPolyHands").SetActive(enabled);
      gameObject.GetChildWithName("HandGizmo").SetActive(!enabled);
    }
  }
  public int GetProgress() { return handSwitchProgress; }
  public void SetProgress(int progress, int switchThreshold)
  {
    handSwitchProgress = progress;
    float progressNormalized = (float)progress / switchThreshold;
    if (progressNormalized <= 0.0f || progressNormalized >= 1.0f)
    {
      SetCanvasEnabled(false);
    }
    else
    {
      SetCanvasEnabled(true);
    }
    Image image = GetImage();
    image.fillAmount = progressNormalized;
    CanvasGroup canvasGroup = GetCanvasGroup();
    canvasGroup.alpha = (progressNormalized) * 0.5f;
  }

  public void SetHandPosRot(Vector3 position, Quaternion rotation)
  {
    if (device == HandTrackingMode.Manus)
    {
      gameObject.transform.position = position;
      gameObject.transform.rotation = rotation;
    }
    else if (device == HandTrackingMode.Leap)
    {
      gameObject.transform.position = position;
      gameObject.transform.rotation = rotation;
    }
  }

  public void SetHandGizmoPosRot(Vector3 position, Quaternion rotation)
  {
    if (device != HandTrackingMode.Leap) return;
    var handGizmo = gameObject.GetChildWithName("HandGizmo");
    handGizmo.transform.position = position;
    handGizmo.transform.rotation = rotation;
  }

  private void SetCanvasEnabled(bool enabled)
  {
    var canvas = GetCanvas();
    canvas.gameObject.SetActive(enabled);
  }

  private Canvas GetCanvas()
  {
    if (device == HandTrackingMode.Manus) return gameObject.GetChildWithName("Canvas").GetComponent<Canvas>();
    if (device == HandTrackingMode.Leap) return gameObject.GetChildWithName("HandGizmo").GetChildWithName("Canvas").GetComponent<Canvas>();
    return null;
  }
  private Image GetImage()
  {
    return GetCanvas().gameObject.GetChildWithName("ProgressIndicator").GetChildWithName("Fill").GetComponent<Image>();
  }
  private CanvasGroup GetCanvasGroup()
  {
    return GetCanvas().gameObject.GetComponent<CanvasGroup>();
  }
  private SkinnedMeshRenderer GetManusRenderer()
  {
    if (device != HandTrackingMode.Manus) return null;
    return gameObject.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetChildWithName("mesh_hand_r").GetComponent<SkinnedMeshRenderer>();
  }
  public HandGrabInteraction GetManusHandGrabInteraction()
  {
    if (device != HandTrackingMode.Manus) return null;
    return gameObject.GetChildWithName("ManusHand_R").GetChildWithName("Interaction").GetComponent<HandGrabInteraction>();
  }
  private HandAnimator GetManusHandAnimator()
  {
    if (device != HandTrackingMode.Manus) return null;
    return gameObject.GetChildWithName("ManusHand_R").GetChildWithName("SK_Hand").GetComponent<HandAnimator>();
  }
}