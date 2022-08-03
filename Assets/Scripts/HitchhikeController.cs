using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViveSR
{
  namespace anipal
  {

    namespace Eye
    {

      public class HitchhikeController : MonoBehaviour
      {
        public GameObject target;
        public GameObject headOrigin;

        // Start is called before the first frame update
        void Start()
        {

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

          if (Physics.Raycast(gazeRay, out var hit))
          {
            target.SetActive(true);
            target.transform.position = hit.point;
          }
          else
          {
            target.SetActive(false);
          }
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