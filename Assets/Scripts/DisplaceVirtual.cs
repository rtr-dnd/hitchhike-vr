using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class DisplaceVirtual : MonoBehaviour
{
  public GameObject RealCounterpart;
  public GameObject tracker;
  public GameObject RealHole;
  public GameObject VirtualHole;
  private Matrix4x4 oMt; // origin to tracker
  private Matrix4x4 tMc; // tracker to chair
  private Matrix4x4 chairMat;
  private Quaternion relativeRot;
  // private SteamVR_Action_Pose tracker1 = SteamVR_Actions.default_Pose;

  // Start is called before the first frame update    

  void Start()
  {
  }

  // Update is called once per frame
  void Update()
  {
    tMc = Matrix4x4.TRS(
        RealCounterpart.transform.localPosition,
        RealCounterpart.transform.localRotation,
        RealCounterpart.transform.localScale
    );
    oMt = Matrix4x4.TRS(
        // tracker1.GetLocalPosition(SteamVR_Input_Sources.RightHand),
        tracker.transform.position,
        // tracker1.GetLocalRotation(SteamVR_Input_Sources.RightHand),
        tracker.transform.rotation,
        new Vector3(1, 1, 1)
    );

    relativeRot = Quaternion.Inverse(VirtualHole.transform.rotation) * RealHole.transform.rotation;
    chairMat =
    Matrix4x4.Translate(VirtualHole.transform.position - RealHole.transform.position) // real to virtual translation
    * Matrix4x4.TRS(
        RealHole.transform.position,
        // Quaternion.FromToRotation(RealHole.transform.forward, VirtualHole.transform.forward),
        // new Quaternion(relativeRot[2], relativeRot[0], relativeRot[1]),
        Quaternion.Euler(-relativeRot.eulerAngles.z, relativeRot.eulerAngles.x, relativeRot.eulerAngles.y), // this somehow works
        Vector3.one
    ) // translation back to real hole and rotation around real hole
    * Matrix4x4.Translate(-RealHole.transform.position) // offset translation for next step
    * oMt * tMc; // real chair

    // Debug.Log(Quaternion.FromToRotation(RealHole.transform.forward, VirtualHole.transform.forward).eulerAngles);
    // Debug.Log(RealHole.transform.rotation.eulerAngles);
    // Debug.Log(VirtualHole.transform.rotation.eulerAngles);
    // Debug.Log((Quaternion.Inverse(VirtualHole.transform.rotation) * RealHole.transform.rotation).eulerAngles);
    // Debug.Log((Quaternion.Inverse(RealHole.transform.rotation) * VirtualHole.transform.rotation).eulerAngles);

    this.transform.position = chairMat.GetColumn(3);
    this.transform.rotation = chairMat.rotation;
    this.transform.localScale = chairMat.lossyScale;
  }
}
