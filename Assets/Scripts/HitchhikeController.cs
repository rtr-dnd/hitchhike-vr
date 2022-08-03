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
        EyeData eye;
        Vector3 CombineGazeRayorigin; // left and right eyes combined gaze
        Vector3 CombineGazeRaydirection;
        Ray CombineRay;
        /*レイがどこに焦点を合わせたかの情報．Vector3 point : 視線ベクトルと物体の衝突位置，float distance : 見ている物体までの距離，
                   Vector3 normal:見ている物体の面の法線ベクトル，Collider collider : 衝突したオブジェクトのCollider，Rigidbody rigidbody：衝突したオブジェクトのRigidbody，Transform transform：衝突したオブジェクトのTransform*/
        //焦点位置にオブジェクトを出すためにpublicにしています．
        public static FocusInfo CombineFocus;
        //レイの半径
        float CombineFocusradius;
        //レイの最大の長さ
        float CombineFocusmaxDistance;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
          if (SRanipal_Eye_API.GetEyeData(ref eye) != ViveSR.Error.WORK)
          {
            Debug.Log("Eye tracking malfunctioning");
            return;
          }

          SRanipal_Eye_API.GetEyeData(ref eye);
          if (SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out CombineGazeRayorigin, out CombineGazeRaydirection, eye))
          {
            Debug.Log("COMBINE GazeRayorigin" + CombineGazeRayorigin.x + ", " + CombineGazeRayorigin.y + ", " + CombineGazeRayorigin.z);
            Debug.Log("COMBINE GazeRaydirection" + CombineGazeRaydirection.x + ", " + CombineGazeRaydirection.y + ", " + CombineGazeRaydirection.z);
            Debug.Log(CombineGazeRaydirection.magnitude);
            target.transform.position = headOrigin.transform.position + CombineGazeRayorigin;
            target.transform.position += CombineGazeRaydirection;
          }
        }
      }

    }
  }
}