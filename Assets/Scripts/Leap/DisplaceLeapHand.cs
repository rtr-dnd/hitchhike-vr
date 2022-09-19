using UnityEngine;
using Leap.Unity;
using Leap;

public class DisplaceLeapHand : PostProcessProvider
{
  public override void ProcessFrame(ref Frame inputFrame)
  {
    foreach (var hand in inputFrame.Hands)
    {
      var originalSpaceOrigin = _inputLeapProvider.transform;
      var copiedSpaceOrigin = gameObject.transform;

      var originalToCopiedRot = Quaternion.Inverse(copiedSpaceOrigin.rotation) * originalSpaceOrigin.rotation;
      var originalToCopiedScale = new Vector3(
        copiedSpaceOrigin.lossyScale.x / originalSpaceOrigin.lossyScale.x,
        copiedSpaceOrigin.lossyScale.y / originalSpaceOrigin.lossyScale.y,
        copiedSpaceOrigin.lossyScale.z / originalSpaceOrigin.lossyScale.z
      );

      var oMh = Matrix4x4.TRS(
        hand.PalmPosition,
        hand.Rotation,
        new Vector3(1, 1, 1)
      ); // origin to hand, equals oMt

      var resMat =
      Matrix4x4.Translate(copiedSpaceOrigin.position - originalSpaceOrigin.position) // orignal to copied translation
      * Matrix4x4.TRS(
          originalSpaceOrigin.position,
          Quaternion.Inverse(originalToCopiedRot),
          originalToCopiedScale
      ) // translation back to original space and rotation & scale around original space
      * Matrix4x4.Translate(-originalSpaceOrigin.position) // offset translation for next step
      * oMh; // tracker

      hand.SetTransform(resMat.GetColumn(3), resMat.rotation);
    }
  }
}