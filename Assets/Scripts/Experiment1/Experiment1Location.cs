using System.Collections.Generic;
using UnityEngine;
using RootScript;
public class Experiment1Location : MonoBehaviour
{
  public enum ExperimentMode
  {
    hitchhike,
    homer
  }


  [SerializeField] GameObject env;
  [SerializeField] GameObject origin;
  [SerializeField] float envDistance = 0.4f;
  [SerializeField] float envBetweenDistance = 1.0f;
  List<GameObject> envs = new List<GameObject>();
  [SerializeField] ExperimentMode mode;
  HitchhikeControllerV3 hitchhike;

  public void ScaleAround(GameObject target, Vector3 pivot, Vector3 newScale)
  {
    Vector3 targetPos = target.transform.position;
    Vector3 diff = targetPos - pivot;
    float relativeScale = newScale.x / target.transform.localScale.x;

    Vector3 resultPos = pivot + diff * relativeScale;
    target.transform.localScale = newScale;
    target.transform.position = resultPos;
  }

  private void Awake()
  {
    if (mode == ExperimentMode.hitchhike)
    {
      GameObject.Find("HitchhikeController").SetActive(true);
      GameObject.Find("ManusHandWrapHitchhike").SetActive(true);
      GameObject.Find("ScaledHOMERController").SetActive(false);
      GameObject.Find("ManusHandWrapHOMER").SetActive(false);
      hitchhike = GameObject.Find("HitchhikeController").GetComponent<HitchhikeControllerV3>();
    }
    else if (mode == ExperimentMode.homer)
    {
      GameObject.Find("HitchhikeController").SetActive(false);
      GameObject.Find("ManusHandWrapHitchhike").SetActive(false);
      GameObject.Find("ScaledHOMERController").SetActive(true);
      GameObject.Find("ManusHandWrapHOMER").SetActive(true);
    }

    envs.Add(env);
    env.transform.position = new Vector3(0, env.transform.position.y, envDistance);
    for (var i = 0; i < 9; i++)
    {
      var tempEnv = GameObject.Instantiate(env, origin.transform.position, env.transform.rotation);
      switch (i / 3)
      {
        case 0:
          tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
          // if (hitchhike.scaleHandWithArea)
          // {
          //   var tempArea = tempEnv.GetChildWithName("HandArea");
          //   ScaleAround(tempArea,
          //     new Vector3(tempArea.transform.position.x, tempArea.transform.position.y - tempArea.transform.lossyScale.y / 2, tempArea.transform.position.z),
          //     tempArea.transform.localScale * 1.2f
          //   );
          // }
          break;
        case 1:
          tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance * 2);
          // if (mode == ExperimentMode.Scale || mode == ExperimentMode.ScaleOnlyArea)
          // {
          //   var tempArea = tempEnv.GetChildWithName("HandArea");
          //   ScaleAround(tempArea,
          //     new Vector3(tempArea.transform.position.x, tempArea.transform.position.y - tempArea.transform.lossyScale.y / 2, tempArea.transform.position.z),
          //     tempArea.transform.localScale * 2f
          //   );
          // }
          break;
        case 2:
          tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance * 3);
          // if (mode == ExperimentMode.Scale || mode == ExperimentMode.ScaleOnlyArea)
          // {
          //   var tempArea = tempEnv.GetChildWithName("HandArea");
          //   ScaleAround(tempArea,
          //     new Vector3(tempArea.transform.position.x, tempArea.transform.position.y - tempArea.transform.lossyScale.y / 2, tempArea.transform.position.z),
          //     tempArea.transform.localScale * 4f
          //   );
          // }
          break;
        case 3:
          tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance * 8);
          // if (mode == ExperimentMode.Scale || mode == ExperimentMode.ScaleOnlyArea)
          // {
          //   var tempArea = tempEnv.GetChildWithName("HandArea");
          //   ScaleAround(tempArea,
          //     new Vector3(tempArea.transform.position.x, tempArea.transform.position.y - tempArea.transform.lossyScale.y / 2, tempArea.transform.position.z),
          //     tempArea.transform.localScale * 8f
          //   );
          // }
          break;
        default:
          break;
      }
      switch (i % 3)
      {
        case 1:
          // tempEnv.transform.RotateAround(origin.transform.position, Vector3.up, 60);
          tempEnv.transform.position += new Vector3(envBetweenDistance, 0, 0);
          break;
        case 2:
          // tempEnv.transform.RotateAround(origin.transform.position, Vector3.up, -60);
          tempEnv.transform.position -= new Vector3(envBetweenDistance, 0, 0);
          break;
        default:
          break;
      }
      envs.Add(tempEnv);

      if (mode == ExperimentMode.hitchhike && tempEnv.GetChildWithName("HandArea") != null) hitchhike.copiedHandAreas.Add(tempEnv.GetChildWithName("HandArea"));
    }


  }
}