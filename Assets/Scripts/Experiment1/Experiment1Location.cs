using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootScript;
using Manus.Interaction;
using System.Text;
using System.IO;
using UnityEngine.InputSystem;


public class Experiment1Location : SingletonMonoBehaviour<Experiment1Location>
{
  public enum ExperimentMode
  {
    hitchhike,
    homer
  }


  [SerializeField] GameObject env;
  [SerializeField] GameObject origin;
  [SerializeField] Transform realHandArea; // real hand area must be in root
  [SerializeField] float realHandMinimumDistance = 0.2f;
  [SerializeField] float realHandMaximumDistance = 0.6f;
  [SerializeField] Transform tracker;
  [SerializeField] float envDistance = 0.4f;
  [SerializeField] float envBetweenDistance = 1.0f;
  [SerializeField] GameObject grabObject;
  [SerializeField] GameObject targetObject;
  [SerializeField] PushButton resetButton;
  [SerializeField] Material inactiveTableMaterial;
  [SerializeField] Material activeTableMaterial;
  [SerializeField] GameObject messagePanel;
  int currentObjectIndex = 0;
  int currentTargetIndex = 0;
  public GameObject currentGrabObjectInstance;
  GameObject currentTargetObjectInstance;
  PushButton currentResetButtonInstance;
  bool[,] ConditionStatus = new bool[6, 6]; // [object, target]
  float previousTime;
  bool finished;
  Vector3 currentTargetLocation = Vector3.zero; // r, phi, rotation; all 0 to 1
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
  private bool frozen;


  public void ScaleAround(GameObject target, Vector3 pivot, Vector3 newScale)
  {
    Vector3 targetPos = target.transform.position;
    Vector3 diff = targetPos - pivot;
    float relativeScale = newScale.x / target.transform.localScale.x;

    Vector3 resultPos = pivot + diff * relativeScale;
    target.transform.localScale = newScale;
    target.transform.position = resultPos;
  }

  protected override void Awake()
  {
    base.Awake();

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
      hitchhike.hideOriginal = true;
      SetFrozen(true);
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

    env.transform.position = new Vector3(0, env.transform.position.y, envDistance);
    for (int i = 0; i < 6; i++)
    {
      var tempEnv = GameObject.Instantiate(env, origin.transform.position, env.transform.rotation);
      tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance);

      switch (i % 3)
      {
        case 1:
          tempEnv.transform.position += new Vector3(envBetweenDistance, 0, 0);
          break;
        case 2:
          tempEnv.transform.position -= new Vector3(envBetweenDistance, 0, 0);
          break;
        default:
          break;
      }
      switch (i / 3)
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

      tempEnv.transform.LookAt(new Vector3(0, env.transform.position.y, 0));
      envs.Add(tempEnv);
      if (mode == ExperimentMode.hitchhike && tempEnv.GetChildWithName("HandArea") != null) hitchhike.copiedHandAreas.Add(tempEnv.GetChildWithName("HandArea"));
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

    InstantiateResetButton();
  }

  void InstantiateResetButton()
  {
    if (currentResetButtonInstance) Destroy(currentResetButtonInstance.gameObject);
    currentResetButtonInstance = GameObject.Instantiate(resetButton.gameObject, resetButton.gameObject.transform.position, resetButton.gameObject.transform.rotation).GetComponent<PushButton>();
    currentResetButtonInstance.onPressed += OnReset;
    var nearestTable = envs[0].GetChildWithName("Table");
    currentResetButtonInstance.transform.position = new Vector3(
      nearestTable.transform.position.x - nearestTable.transform.lossyScale.x / 2,
      nearestTable.transform.position.y,
      nearestTable.transform.position.z - nearestTable.transform.lossyScale.z / 2
    );

    resetButton.gameObject.SetActive(false);
    currentResetButtonInstance.gameObject.SetActive(true);
  }

  void StartCondition()
  {
    foreach (var env in envs)
    {
      env.GetChildWithName("Table").GetComponent<MeshRenderer>().material = inactiveTableMaterial;
    }
    envs[currentObjectIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material = activeTableMaterial;
    envs[currentTargetIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material = activeTableMaterial;

    if (currentGrabObjectInstance != null)
    {
      HandWrap wrap = null;
      if (mode == ExperimentMode.homer) wrap = homer.handWrap;
      if (mode == ExperimentMode.hitchhike) wrap = hitchhike.activeHandWrap;
      wrap.GetManusHandGrabInteraction().Release();
      Destroy(currentGrabObjectInstance);
    }
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
    Debug.Log("reset button pressed");
    OnReset();
  }

  void OnReset()
  {
    if (status == Status.beforeInitialReset)
    {
      status = Status.reaching;
      InitializeCondition();
      StartCondition();
      return;
    }

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

    var trialNum = 1;
    foreach (var i in ConditionStatus)
    {
      if (i) trialNum++;
    }
    if (trialNum >= ConditionStatus.GetLength(0) * ConditionStatus.GetLength(1))
    {
      Debug.Log("Finished all condition");
      messagePanel.SetActive(true);
      return;
    }

    var flag = true;
    while (flag)
    {
      currentObjectIndex = Random.Range(0, envs.Count);
      currentTargetIndex = Random.Range(0, envs.Count);
      if (!ConditionStatus[currentObjectIndex, currentTargetIndex])
      {
        flag = false; // found uncompleted condition
        ConditionStatus[currentObjectIndex, currentTargetIndex] = true;
      }
    }


    Debug.Log("Trial " + trialNum + ": object " + currentObjectIndex + ", target " + currentTargetIndex);

    currentTargetLocation = new Vector3(
      Random.Range(0f, 1f),
      Random.Range(0f, 1f),
      Random.Range(0f, 1f)
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
      0,
      0,
      currentTargetLocation.x * desk.lossyScale.z / 2 * 0.8f // prevents sticking out
    );
    currentTargetObjectInstance.transform.RotateAround(desk.transform.position, Vector3.up, currentTargetLocation.y * 360);
    currentTargetObjectInstance.transform.Rotate(Vector3.forward, currentTargetLocation.z * 360);
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
    sb = new StringBuilder("time, handPositionX, handPositionY, ");
  }

  private void Update()
  {
    HandleKeyboardEvent();

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

    if (!isOK) return;
    envs[currentTargetIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material.color = Color.blue;
    status = Status.completed;

    if (finished) return;
    Debug.Log(Time.time - previousTime);
    finished = true;
  }

  void HandleKeyboardEvent()
  {
    var keyboard = Keyboard.current;

    // f for freeze
    if (keyboard.fKey.wasPressedThisFrame) SetFrozen(!frozen);

    // r for reset
    if (keyboard.rKey.wasPressedThisFrame)
    {
      InstantiateResetButton();
      OnReset();
    }

    // if (keyboard.gKey.wasPressedThisFrame) giveUp(); // todo

    // d for set hand distance
    if (keyboard.dKey.wasPressedThisFrame && mode == ExperimentMode.hitchhike)
    {
      if (keyboard.shiftKey.isPressed)
      {
        SetMaximumHandDistance(); // large R for maximum hand distance
      }
      else
      {
        SetMinimumHandDistance(); // small r for minimum hand distance
      }
    }
  }

  void SetFrozen(bool a_frozen)
  {
    frozen = a_frozen;
    messagePanel.SetActive(a_frozen);
    Debug.Log("frozen: " + a_frozen);
    if (mode == ExperimentMode.hitchhike) SetHitchhikeFrozen(a_frozen);
  }

  void SetHitchhikeFrozen(bool a_frozen)
  {
    if (hitchhike == null) return;
    hitchhike.frozen = a_frozen;
  }

  void SetMinimumHandDistance()
  {
    realHandMinimumDistance = tracker.position.z;
    Debug.Log("Set minimum hand distance: " + realHandMinimumDistance);
    SetRealHandArea();
  }
  void SetMaximumHandDistance()
  {
    realHandMaximumDistance = tracker.position.z;
    Debug.Log("Set maximum hand distance: " + realHandMaximumDistance);
    SetRealHandArea();
  }

  void SetRealHandArea()
  {
    // realHandArea.transform.position = new Vector3(
    //   realHandArea.transform.position.x,
    //   realHandArea.transform.position.y,
    //   realHandMinimumDistance + (realHandMaximumDistance - realHandMinimumDistance) / 2
    // );
    // realHandArea.transform.localScale = new Vector3( // realhandarea must be in root
    //   realHandMaximumDistance - realHandMinimumDistance,
    //   realHandArea.transform.localScale.y,
    //   realHandMaximumDistance - realHandMinimumDistance
    // );
    realHandArea.transform.position = new Vector3(
          realHandArea.transform.position.x,
          realHandArea.transform.position.y,
          realHandMinimumDistance + (realHandMaximumDistance - realHandMinimumDistance) / 2
        );
    realHandArea.transform.localScale = new Vector3( // realhandarea must be in root
      realHandArea.transform.localScale.x,
      realHandArea.transform.localScale.y,
      realHandMaximumDistance - realHandMinimumDistance
    );

    env.GetChildWithName("HandArea").transform.position = realHandArea.transform.position;
    env.GetChildWithName("HandArea").transform.localScale = new Vector3(
      realHandArea.transform.lossyScale.x / env.transform.lossyScale.x,
      realHandArea.transform.lossyScale.y / env.transform.lossyScale.y,
      realHandArea.transform.lossyScale.z / env.transform.lossyScale.z
    );
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