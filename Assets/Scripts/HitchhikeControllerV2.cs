using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manus.Hand;
using RootScript;

namespace ViveSR
{
  namespace anipal
  {

    namespace Eye
    {

      public class HitchhikeControllerV2 : MonoBehaviour
      {
        public GameObject originalHandWrap;
        public GameObject originalHandArea;
        private List<GameObject> copiedHandWraps;
        public List<GameObject> copiedHandAreas;
        public GameObject headOrigin;
        public GameObject tracker;
        public Material enabledMaterial;
        public Material disabledMaterial;
        private int maxRaycastDistance = 100;
        private int activeHandIndex = 0; // 0: original, 1~: copied

        // Start is called before the first frame update
        void Start()
        {
          copiedHandWraps = new List<GameObject>();
          foreach (var item in copiedHandAreas)
          {
            copiedHandWraps.Add(GameObject.Instantiate(originalHandWrap, item.transform.position, item.transform.rotation));
          }
          foreach (var item in copiedHandWraps)
          {
            SetWrapEnabled(item, false);
          }
        }

        // Update is called once per frame
        void Update()
        {
          if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
          {
            Debug.Log("Eye tracking malfunctioning");
            return;
          }

          var gazeRay = GetGazeRay();

          int layerMask = LayerMask.GetMask("Hitchhike");
          if (Physics.Raycast(gazeRay, out var hit, maxRaycastDistance, layerMask))
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
              // if (activeHandIndex == 0)
              // {
              //   activeHandRelativePos = Vector3.zero;
              // }
              // else
              // {
              //   activeHandRelativePos = copiedHandWraps[activeHandIndex - 1].transform.position - tracker.transform.position;
              // }
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
            // copiedHandWraps[activeHandIndex - 1].transform.position = tracker.transform.position + activeHandRelativePos;
            // copiedHandWraps[activeHandIndex - 1].transform.rotation = tracker.transform.rotation;
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
          var eyeData = GetCombinedSingleEyeData();
          var gazeDirection = eyeData.gaze_direction_normalized;
          gazeDirection.x *= -1; // right hand coor to left hand coor

          return new Ray(headOrigin.transform.position, headOrigin.transform.rotation * gazeDirection);
        }

        private SingleEyeData GetCombinedSingleEyeData()
        {
          var eyeData = SRanipal_Eye_v2.GetVerboseData(out var verboseData);
          return verboseData.combined.eye_data;
        }
      }

    }
  }
}