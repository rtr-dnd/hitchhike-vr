using System.Collections.Generic;
using UnityEngine;
using RootScript;
using Manus.Interaction;
using System.Text;
using System.IO;

public class Experiment1Location : MonoBehaviour
{
  public enum ExperimentMode
  {
    hitchhike,
    homer
  }


  [SerializeField] GameObject env;
  [SerializeField] GameObject origin;
  [SerializeField] Transform realHandArea;
  [SerializeField] float envDistance = 0.4f;
  [SerializeField] float envBetweenDistance = 1.0f;
  [SerializeField] GameObject grabObject;
  [SerializeField] GameObject targetObject;
  [SerializeField] PushButton resetButton;
  [SerializeField] Material inactiveTableMaterial;
  [SerializeField] Material activeTableMaterial;
  int currentObjectIndex = 0;
  int currentTargetIndex = 0;
  GameObject currentGrabObjectInstance;
  GameObject currentTargetObjectInstance;
  float previousTime;
  bool finished;
  Vector3 currentTargetLocation = Vector3.zero; // x, y, rotation; all -1 to 1
  List<GameObject> envs = new List<GameObject>();
  [SerializeField] ExperimentMode mode;
  HitchhikeControllerV3 hitchhike;
  ScaledHOMERController homer;
  private StringBuilder sb;
  enum Status
  { // 0: until initial reset button, 1: reset button -> grab object, 2: grab object -> place object, 3: place object -> reset button
    beforeInitialReset,
    reaching,
    placing,
    completed
  }
  Status status;
  long startTimeStamp;


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
    status = Status.beforeInitialReset;
    if (mode == ExperimentMode.hitchhike)
    {
      GameObject.Find("HitchhikeController").SetActive(true);
      GameObject.Find("ManusHandWrapHitchhike").SetActive(true);
      GameObject.Find("ScaledHOMERController").SetActive(false);
      GameObject.Find("ManusHandWrapHOMER").SetActive(false);
      hitchhike = GameObject.Find("HitchhikeController").GetComponent<HitchhikeControllerV3>();
      hitchhike.onRelease += OnRelease;
      hitchhike.onGrab += OnGrab;
    }
    else if (mode == ExperimentMode.homer)
    {
      GameObject.Find("HitchhikeController").SetActive(false);
      GameObject.Find("ManusHandWrapHitchhike").SetActive(false);
      GameObject.Find("ScaledHOMERController").SetActive(true);
      GameObject.Find("ManusHandWrapHOMER").SetActive(true);
      homer = GameObject.Find("ScaledHOMERController").GetComponent<ScaledHOMERController>();
      homer.onRelease += OnRelease;
      homer.onGrab += OnGrab;
    }

    env.transform.position = new Vector3(0, env.transform.position.y, 0.1f);
    for (int i = 0; i < 27; i++)
    {
      var tempEnv = GameObject.Instantiate(env, origin.transform.position, env.transform.rotation);
      // switch (i)
      // { // tetrahedron like
      //   case 0:
      //     tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
      //     break;
      //   case 1:
      //     tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
      //     tempEnv.transform.RotateAround(new Vector3(0, env.transform.position.y, envDistance), Vector3.up, -45);
      //     tempEnv.transform.rotation = env.transform.rotation;
      //     break;
      //   case 2:
      //     tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
      //     tempEnv.transform.RotateAround(new Vector3(0, env.transform.position.y, envDistance), Vector3.up, 45);
      //     tempEnv.transform.rotation = env.transform.rotation;
      //     break;
      //   case 3:
      //     tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
      //     tempEnv.transform.RotateAround(new Vector3(0, env.transform.position.y, envDistance), Vector3.right, 45);
      //     tempEnv.transform.rotation = env.transform.rotation;
      //     break;
      //   case 4:
      //     tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
      //     tempEnv.transform.RotateAround(new Vector3(0, env.transform.position.y, envDistance), Vector3.right, -45);
      //     tempEnv.transform.rotation = env.transform.rotation;
      //     break;
      // }
      switch ((i / 3) % 3)
      {
        case 0:
          // tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance);
          tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance);

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
          // tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance * 2);
          tempEnv.transform.position = new Vector3(0, env.transform.position.y + envBetweenDistance, envDistance);

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
          // tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance + envBetweenDistance * 3);
          tempEnv.transform.position = new Vector3(0, env.transform.position.y - envBetweenDistance, envDistance);
          // if (mode == ExperimentMode.Scale || mode == ExperimentMode.ScaleOnlyArea)
          // {
          //   var tempArea = tempEnv.GetChildWithName("HandArea");
          //   ScaleAround(tempArea,
          //     new Vector3(tempArea.transform.position.x, tempArea.transform.position.y - tempArea.transform.lossyScale.y / 2, tempArea.transform.position.z),
          //     tempArea.transform.localScale * 4f
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
      switch (i / 9)
      {
        case 0:
          tempEnv.transform.position += new Vector3(0, 0, 0);
          break;
        case 1:
          tempEnv.transform.position += new Vector3(0, 0, envBetweenDistance);
          break;
        case 2:
          tempEnv.transform.position += new Vector3(0, 0, envBetweenDistance * 2);
          break;
      }
      envs.Add(tempEnv);

      if (mode == ExperimentMode.hitchhike && tempEnv.GetChildWithName("HandArea") != null && i != 0) hitchhike.copiedHandAreas.Add(tempEnv.GetChildWithName("HandArea"));
    }

    env.GetChildWithName("HandArea").transform.position = realHandArea.transform.position;
    env.GetChildWithName("HandArea").transform.localScale = new Vector3(
      realHandArea.transform.lossyScale.x / env.transform.lossyScale.x,
      realHandArea.transform.lossyScale.y / env.transform.lossyScale.y,
      realHandArea.transform.lossyScale.z / env.transform.lossyScale.z
    );
    env.SetActive(false);

    grabObject.SetActive(false);
    targetObject.SetActive(false);
    resetButton.onPressed += OnReset;

    InitializeCondition();
    StartCondition();
  }

  void StartCondition()
  {
    foreach (var env in envs)
    {
      env.GetChildWithName("Table").GetComponent<MeshRenderer>().material = inactiveTableMaterial;
    }
    envs[currentObjectIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material = activeTableMaterial;
    envs[currentTargetIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material = activeTableMaterial;

    if (currentGrabObjectInstance != null) Destroy(currentGrabObjectInstance);
    currentGrabObjectInstance = GameObject.Instantiate(grabObject);
    currentGrabObjectInstance.SetActive(true);
    PlaceObject(envs[currentObjectIndex]);

    if (currentTargetObjectInstance != null) Destroy(currentTargetObjectInstance);
    currentTargetObjectInstance = GameObject.Instantiate(targetObject);
    currentTargetObjectInstance.SetActive(true);
    PlaceTarget(envs[currentTargetIndex]);
  }

  void OnReset(PushButton p)
  {
    var isOK = true;
    if (currentTargetObjectInstance == null) return;
    foreach (var item in currentTargetObjectInstance.GetComponentsInChildren<DetectPosition>())
    {
      if (!item.GetOK()) isOK = false;
    }

    if (isOK)
    {
      // all collider is ok -> next condition
      // Debug.Log(Time.time - previousTime);
      InitializeCondition();
      StartCondition();
    }
    else
    {
      Debug.Log("reset");
      StartCondition();
    }
    status = Status.reaching;
  }

  void InitializeCondition()
  {
    previousTime = Time.time;
    finished = false;
    currentObjectIndex = Random.Range(0, envs.Count);
    currentTargetIndex = Random.Range(0, envs.Count);
    currentTargetLocation = new Vector3(
      Random.Range(-1f, 1f),
      Random.Range(-1f, 1f),
      Random.Range(-1f, 1f)
    );
  }

  void PlaceTarget(GameObject env)
  {
    var desk = env.GetChildWithName("Table").transform;
    currentTargetObjectInstance.transform.position = new Vector3(
      desk.position.x,
      desk.position.y + desk.lossyScale.y / 2,
      desk.position.z
    );
    currentTargetObjectInstance.transform.position += new Vector3(
      currentTargetLocation.x * desk.lossyScale.x / 2 * 0.8f, // prevents sticking out
      0,
      currentTargetLocation.y * desk.lossyScale.z / 2 * 0.8f
    );
    currentTargetObjectInstance.transform.Rotate(Vector3.forward, currentTargetLocation.z * 180);
  }

  void PlaceObject(GameObject env)
  {
    var desk = env.GetChildWithName("Table").transform;
    currentGrabObjectInstance.transform.position = new Vector3(
      desk.position.x,
      desk.position.y + desk.lossyScale.y / 2 + 0.1f,
      desk.position.z
    );
  }

  private void Start()
  {
    System.DateTime centuryBegin = new System.DateTime(2001, 1, 1);
    startTimeStamp = System.DateTime.Now.Ticks - centuryBegin.Ticks;
    sb = new StringBuilder("time, ");
  }

  private void Update()
  {
    sb.Append("\n")
    .Append(Time.fixedTimeAsDouble).Append("\n")
    .Append(logOf(mode == ExperimentMode.hitchhike ? hitchhike.activeHandWrap.gameObject : homer.handWrap.gameObject)).Append("\n");

    if (currentTargetObjectInstance == null) return;
    if (finished) return;
    bool isGrabbing = mode == ExperimentMode.hitchhike ? hitchhike.isGrabbing : homer.isGrabbing;
    if (isGrabbing) return;

    var isOK = true;
    foreach (var item in currentTargetObjectInstance.GetComponentsInChildren<DetectPosition>())
    {
      if (!item.GetOK()) isOK = false;
    }

    if (isOK)
    {
      Debug.Log(Time.time - previousTime);
      envs[currentTargetIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material.color = Color.blue;
      finished = true;
      status = Status.completed;
    }
  }

  string logOf(GameObject obj)
  {
    return (
        obj.transform.position.x + ", "
        + obj.transform.position.y + ", "
        + obj.transform.position.z + ", "
        + obj.transform.eulerAngles.x + ", "
        + obj.transform.eulerAngles.y + ", "
        + obj.transform.eulerAngles.z
        );
  }

  private void OnDestroy()
  {
    var folder = Application.persistentDataPath;

    var filePath = Path.Combine(folder, $"test_{startTimeStamp}.csv");
    using (var writer = new StreamWriter(filePath, false))
    {
      writer.Write(sb.ToString());
      Debug.Log("written results");
    }
  }

  void OnRelease(HandWrap wrap)
  {
    // Debug.Log("on release");
  }

  void OnGrab(HandWrap wrap)
  {
    if (status != Status.reaching && status != Status.beforeInitialReset) return;
    if (wrap.GetManusHandGrabInteraction().grabbedObject.gameObject == currentGrabObjectInstance) status = Status.placing;
  }
}