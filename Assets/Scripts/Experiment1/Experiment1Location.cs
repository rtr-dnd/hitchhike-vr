using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootScript;
using Manus.Interaction;
using System.Text;
using System.IO;
using UnityEngine.InputSystem;
using ViveSR.anipal.Eye;

public class Experiment1Location : SingletonMonoBehaviour<Experiment1Location>
{
  public enum ExperimentMode
  {
    hitchhike,
    homer
  }
  public enum Status
  { // 0: until initial reset button, 1: reset button -> grab object, 2: grab object -> place object, 3: place object -> reset button
    beforeInitialReset,
    reaching,
    placing,
    completed
  }

  [SerializeField] GameObject env;
  [SerializeField] GameObject origin;
  [SerializeField] Transform realHandArea; // real hand area must be in root
  [SerializeField] Transform tracker;
  [SerializeField] Transform head;
  [SerializeField] float envDistance = 0.4f;
  [SerializeField] float envBetweenDistance = 1.0f;
  [SerializeField] GameObject grabObject;
  [SerializeField] GameObject targetObject;
  [SerializeField] float placementDistanceThreshold = 0.05f;
  [SerializeField] float placementAngleThreshold = 10f;
  [SerializeField] PushButton resetButton;
  [SerializeField] Material inactiveTableMaterial;
  [SerializeField] Material activeTableMaterial;
  [SerializeField] GameObject messagePanel;
  [SerializeField] Manus.Hand.Gesture.GestureBase grabGesture;
  float realHandMinimumDistance = 0.2f;
  float realHandMaximumDistance = 0.6f;
  int currentObjectIndex = 0;
  int currentTargetIndex = 0;
  public GameObject currentGrabObjectInstance;
  GameObject currentTargetObjectInstance;
  PushButton currentResetButtonInstance;
  bool[,] ConditionStatus = new bool[7, 7]; // [object, target]
  float lastResetTime;
  float lastReachedTime;
  float lastPlacedTime;
  bool finished;
  bool isCorrectlyPlaced;
  Vector3 currentTargetLocation = Vector3.zero; // r, phi, rotation; all 0 to 1
  List<GameObject> envs = new List<GameObject>();
  [SerializeField] ExperimentMode mode;
  HitchhikeControllerV3 hitchhike;
  ScaledHOMERController homer;

  Status status;
  long startTimeStamp;
  private bool frozen;
  Color defaultGizmoColor;

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
    SetFrozen(true);
    defaultGizmoColor = targetObject.GetComponentInChildren<TargetGizmo>().gameObject.GetComponentInChildren<MeshRenderer>().material.color;
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
    for (int i = 0; i < 7; i++)
    {
      var tempEnv = GameObject.Instantiate(env, origin.transform.position, env.transform.rotation);
      tempEnv.transform.position = new Vector3(0, env.transform.position.y, envDistance);
      if (i == 0)
      {
        envs.Add(tempEnv);
        if (mode == ExperimentMode.hitchhike && tempEnv.GetChildWithName("HandArea") != null) hitchhike.copiedHandAreas.Add(tempEnv.GetChildWithName("HandArea"));
        continue;
      }

      switch ((i - 1) / 3)
      {
        case 0:
          tempEnv.transform.position += new Vector3(0, 0, envBetweenDistance);
          break;
        case 1:
          tempEnv.transform.position += new Vector3(0, 0, envBetweenDistance * 2);
          break;
        default:
          break;
      }
      switch (i % 3)
      {
        case 0:
          break;
        case 1:
          tempEnv.transform.RotateAround(origin.transform.position, Vector3.up, 45);
          break;
        case 2:
          tempEnv.transform.RotateAround(origin.transform.position, Vector3.up, -45);
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

    if (status == Status.completed)
    {
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
    lastResetTime = Time.time;
    finished = false;

    if (GetTrialNum() >= ConditionStatus.GetLength(0) * (ConditionStatus.GetLength(1) - 1)) // excluding the conditions when start and goal is same
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
      if (currentObjectIndex == currentTargetIndex) continue; // skip if start and goal is same
      if (!ConditionStatus[currentObjectIndex, currentTargetIndex])
      {
        flag = false; // found uncompleted condition
        ConditionStatus[currentObjectIndex, currentTargetIndex] = true;
      }
    }


    Debug.Log("Trial " + (GetTrialNum() + 1) + ": object " + currentObjectIndex + ", target " + currentTargetIndex);

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
  }

  private void Update()
  {
    HandleKeyboardEvent();

    if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
    {
      Debug.Log("Eye tracking malfunctioning");
      return;
    }

    // log per frame
    LoggerPerFrame.Instance.DataList.Add(new LoggerPerFrame.Data(
      Time.fixedTimeAsDouble,
      mode,
      tracker.transform,
      mode == ExperimentMode.hitchhike ? hitchhike.activeHandWrap.GetHandCenter() : homer.handWrap.GetHandCenter(),
      realHandArea.transform,
      currentGrabObjectInstance ? currentGrabObjectInstance.transform : null,
      currentTargetObjectInstance ? currentTargetObjectInstance.transform : null,
      head.transform,
      GetGazeDirection(),
      head.transform.position + GetGazeDirection() * GetFocusDistance(),
      currentObjectIndex,
      currentTargetIndex,
      currentResetButtonInstance.pressed,
      isCorrectlyPlaced,
      grabGesture.Evaluate(mode == ExperimentMode.hitchhike ? hitchhike.activeHandWrap.GetManusHand() : homer.handWrap.GetManusHand()),
      mode == ExperimentMode.hitchhike ? hitchhike.activeHandWrap.GetManusHandGrabInteraction().grabbedObject != null : homer.handWrap.GetManusHandGrabInteraction().grabbedObject != null,
      frozen,
      status
    ));

    if (currentTargetObjectInstance == null) return;
    if (finished) return;

    // placement detection
    var targetGizmo = currentTargetObjectInstance.GetComponentInChildren<TargetGizmo>();
    var distance = Vector3.Distance(currentGrabObjectInstance.transform.position, targetGizmo.transform.position);
    var angle = Vector3.Angle(currentGrabObjectInstance.transform.forward, targetGizmo.transform.forward);

    isCorrectlyPlaced = distance < placementDistanceThreshold && angle < placementAngleThreshold; // todo: angle requirement
    targetGizmo.GetComponentInChildren<MeshRenderer>().material.color = isCorrectlyPlaced ? Color.blue : defaultGizmoColor;

    bool isGrabbing = mode == ExperimentMode.hitchhike ? hitchhike.isGrabbing : homer.isGrabbing;
    if (isGrabbing) return;

    // var isOK = true;
    // foreach (var item in currentTargetObjectInstance.GetComponentsInChildren<DetectPosition>())
    // {
    //   if (!item.GetOK()) isOK = false;
    // }

    // if (!isOK) return;
    if (!isCorrectlyPlaced) return;
    envs[currentTargetIndex].GetChildWithName("Table").GetComponent<MeshRenderer>().material.color = Color.blue;
    status = Status.completed;

    if (finished) return;
    lastPlacedTime = Time.time;
    finished = true;

    // log per condition
    Debug.Log("reaching time: " + (lastReachedTime - lastResetTime) + ", placing time: " + (lastPlacedTime - lastReachedTime));
    LoggerPerCondition.Instance.DataList.Add(new LoggerPerCondition.Data(
      Time.fixedTimeAsDouble,
      mode,
      GetTrialNum(),
      lastReachedTime - lastResetTime,
      lastPlacedTime - lastReachedTime,
      currentObjectIndex,
      currentTargetIndex,
      currentTargetLocation
    ));
  }

  int GetTrialNum()
  {
    var trialNum = 0;
    foreach (var i in ConditionStatus)
    {
      if (i) trialNum++;
    }
    return trialNum;
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

  void OnRelease(HandWrap wrap)
  {
    // Debug.Log("on release");
  }

  void OnGrab(HandWrap wrap)
  {
    if (status != Status.reaching && status != Status.beforeInitialReset) return;
    if (wrap.GetManusHandGrabInteraction().grabbedObject.gameObject == currentGrabObjectInstance)
    {
      status = Status.placing;
      if (lastReachedTime < lastResetTime) lastReachedTime = Time.time; // detect initial reaching only
    }
  }

  private Vector3 GetGazeDirection()
  {
    var eyeData = GetCombinedSingleEyeData();
    var gazeDirection = eyeData.gaze_direction_normalized;
    gazeDirection.x *= -1; // right hand coor to left hand coor

    var direction = head.transform.rotation * gazeDirection;
    return direction.normalized;
  }

  private SingleEyeData GetCombinedSingleEyeData()
  {
    var eyeData = SRanipal_Eye_v2.GetVerboseData(out var verboseData);
    return verboseData.combined.eye_data;
  }

  private float GetFocusDistance()
  {
    var wasSuccess = SRanipal_Eye_v2.Focus(GazeIndex.COMBINE, out var combineRay, out var combineFocus, 0, float.MaxValue);
    return combineFocus.distance;
  }
}