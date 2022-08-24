using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Manus.Hand;
using RootScript;

namespace ViveSR
{
  namespace anipal
  {

    namespace Eye
    {

      public class HitchhikeControllerV3 : MonoBehaviour
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
        private float raycastDistanceAcceptThreshold = 0.1f;
        private int activeHandIndex = 0; // 0: original, 1~: copied
        private List<int> handSwitchProgress;
        private int handSwitchProgressThreshold = 70;

        // Start is called before the first frame update
        void Start()
        {
          copiedHandWraps = new List<GameObject>();
          handSwitchProgress = new List<int>();
          handSwitchProgress.Add(0);
          foreach (var item in copiedHandAreas)
          {
            var initialHandPosition = item.GetChildWithName("InitialHandPosition");
            copiedHandWraps.Add(GameObject.Instantiate(originalHandWrap, initialHandPosition.transform.position, initialHandPosition.transform.rotation));
            handSwitchProgress.Add(0);
          }
          SetProgressOfWrap(originalHandWrap, 0);
          foreach (var item in copiedHandWraps)
          {
            SetWrapEnabled(item, false);
            SetProgressOfWrap(item, 0);
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
          var focusDistance = GetFocusDistance();

          int layerMask = LayerMask.GetMask("Hitchhike");
          RaycastHit closestHit = new RaycastHit();
          float closestDistance = float.PositiveInfinity;
          foreach (var hit in Physics.RaycastAll(gazeRay, Mathf.Min(maxRaycastDistance, focusDistance), layerMask))
          {
            // finding a hit that's closest to focus point
            if (hit.distance - focusDistance > raycastDistanceAcceptThreshold) continue;
            if (Mathf.Abs(hit.distance - focusDistance) < Mathf.Abs(closestDistance - focusDistance))
            {
              closestHit = hit;
              closestDistance = hit.distance;
            }
          }

          var currentGazeIndex = -1;
          if (closestDistance < float.PositiveInfinity) currentGazeIndex = GetNewHandIndex(closestHit);

          // update hand status
          for (var i = 0; i < handSwitchProgress.Count; i++)
          {
            if (i == currentGazeIndex && currentGazeIndex != activeHandIndex)
            {
              handSwitchProgress[i] += 3;
              if (i == 0)
              {
                SetProgressOfWrap(originalHandWrap, (float)handSwitchProgress[i] / handSwitchProgressThreshold);
              }
              else
              {
                SetProgressOfWrap(copiedHandWraps[i - 1], (float)handSwitchProgress[i] / handSwitchProgressThreshold);
              }
            }
            else if (handSwitchProgress[i] > 0)
            {
              handSwitchProgress[i] -= 1;
              if (i == 0)
              {
                SetProgressOfWrap(originalHandWrap, (float)handSwitchProgress[i] / handSwitchProgressThreshold);
              }
              else
              {
                SetProgressOfWrap(copiedHandWraps[i - 1], (float)handSwitchProgress[i] / handSwitchProgressThreshold);
              }
            }
          }
          // Debug.Log(string.Join(", ", handSwitchProgress.Select(i => i.ToString())));

          // switch hand operation
          for (var i = 0; i < handSwitchProgress.Count; i++)
          {
            if (handSwitchProgress[i] < handSwitchProgressThreshold) continue;
            if (i == activeHandIndex) continue;

            handSwitchProgress[i] = 0;
            if (i == 0)
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
              SetWrapEnabled(copiedHandWraps[i - 1], true);
            }
            activeHandIndex = i;
          }

          // update hand pos
          if (activeHandIndex == 0)
          {
            originalHandWrap.transform.position = tracker.transform.position;
            originalHandWrap.transform.rotation = tracker.transform.rotation;
          }
          else
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

        private void SetWrapCanvasEnabled(GameObject wrap, bool enabled)
        {
          var canvas = wrap.GetChildWithName("Canvas");
          canvas.SetActive(enabled);
        }
        private void SetProgressOfWrap(GameObject wrap, float progress)
        {
          if (progress <= 0.0f || progress >= 1.0f)
          {
            SetWrapCanvasEnabled(wrap, false);
          }
          else
          {
            SetWrapCanvasEnabled(wrap, true);
          }
          var image = wrap.GetChildWithName("Canvas").GetChildWithName("ProgressIndicator").GetChildWithName("Fill").GetComponent<Image>();
          image.fillAmount = progress;
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

        private float GetFocusDistance()
        {
          var wasSuccess = SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out var combineRay, out var combineFocus);
          return combineFocus.distance;
        }
      }

    }
  }
}