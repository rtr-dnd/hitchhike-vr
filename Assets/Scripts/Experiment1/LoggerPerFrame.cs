using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static Experiment1Location;

public class LoggerPerFrame : SingletonMonoBehaviour<LoggerPerFrame> // シングルトンは調べてください
{
  public struct TransformSt
  {
    public Vector3? position;
    public Vector3? eulerAngles;
    public Vector3? lossyScale;
    public TransformSt(Transform transform)
    {
      if (transform == null)
      {
        position = null; eulerAngles = null; lossyScale = null; return;
      }
      position = transform.position;
      eulerAngles = transform.eulerAngles;
      lossyScale = transform.lossyScale;
    }
  }
  // データ保持用構造体
  public struct Data
  {
    public double time;
    public ExperimentMode mode;
    public TransformSt tracker;
    public TransformSt activeHand;
    public TransformSt realHandArea;
    public TransformSt car;
    public TransformSt parkingLot;
    public TransformSt head;
    public Vector3 gazeDirection;
    public Vector3 gazeHitPoint;
    public int currentObjectIndex;
    public int currentTargetIndex;
    public bool resetButtonIsPressed;
    // todo: 当たり判定
    public bool isGrabbingGesture;
    public bool isGrabbingObject;
    public bool isFrozen;
    public Status status;

    // コンストラクタ
    public Data(
      double a_time,
      ExperimentMode a_mode,
      Transform a_tracker,
      Transform a_activeHand,
      Transform a_realHandArea,
      Transform a_car,
      Transform a_parkingLot,
      Transform a_head,
      Vector3 a_gazeDirection,
      Vector3 a_gazeHitPoint,
      int a_currentObjectIndex,
      int a_currentTargetIndex,
      bool a_resetButtonIsPressed,
      // todo: 当たり判定
      bool a_isGrabbingGesture,
      bool a_isGrabbingObject,
      bool a_isFrozen,
      Status a_status)
    {
      time = a_time;
      mode = a_mode;
      tracker = new TransformSt(a_tracker);
      activeHand = new TransformSt(a_activeHand);
      realHandArea = new TransformSt(a_realHandArea);
      car = new TransformSt(a_car);
      parkingLot = new TransformSt(a_parkingLot);
      head = new TransformSt(a_head);
      gazeDirection = a_gazeDirection;
      gazeHitPoint = a_gazeHitPoint;
      currentObjectIndex = a_currentObjectIndex;
      currentTargetIndex = a_currentTargetIndex;
      resetButtonIsPressed = a_resetButtonIsPressed;
      // todo: 当たり判定
      isGrabbingGesture = a_isGrabbingGesture;
      isGrabbingObject = a_isGrabbingObject;
      isFrozen = a_isFrozen;
      status = a_status;
    }

    // CSVの1行目用
    public static string Header
    {
      get
      {
        return $@"
          time,
          mode,
          {hTransform("tracker", true, true, false)}
          {hTransform("activeHand", true, true, false)}
          {hTransform("realHandArea", true, false, true)}
          {hTransform("car", true, true, false)}
          {hTransform("parkingLot", true, true, false)}
          {hTransform("head", true, true, false)}
          gazeDirection,
          gazeHitPoint,
          currentObjectIndex,
          currentTargetIndex,
          resetButtonIsPressed,
          // todo: 当たり判定
          isGrabbingGesture,
          isGrabbingObject,
          isFrozen,
          status,
        ".Replace(Environment.NewLine, "");
      }
    }

    public static string hTransform(string name, bool includePos, bool includeRot, bool includeSca)
    {
      var pos = $"{name}Pos, ";
      var rot = $"{name}Rot, ";
      var sca = $"{name}Sca, ";
      return (includePos ? pos : "")
      + (includeRot ? rot : "")
      + (includeSca ? sca : "");
    }

    // stringへのキャストをオーバーロード
    // CSVの2行目以降を構成するように各要素をコンマで繋げる
    public override string ToString()
    {
      return $@"
{time},
{mode},
{t(tracker, true, true, false)}
{t(activeHand, true, true, false)}
{t(realHandArea, true, false, true)}
{t(car, true, true, false)}
{t(parkingLot, true, true, false)}
{t(head, true, true, false)}
{t(gazeDirection)},
{t(gazeHitPoint)},
{currentObjectIndex},
{currentTargetIndex},
{resetButtonIsPressed},
{isGrabbingGesture},
{isGrabbingObject},
{isFrozen},
{status},".Replace(Environment.NewLine, "");
    }

    public static string t(TransformSt tr, bool includePos, bool includeRot, bool includeSca)
    {
      var pos = $"{(tr.position == null ? "null" : t(tr.position.Value))}, ";
      var rot = $"{(tr.eulerAngles == null ? "null" : t(tr.eulerAngles.Value))}, ";
      var sca = $"{(tr.lossyScale == null ? "null" : t(tr.lossyScale.Value))}, ";
      return (includePos ? pos : "")
      + (includeRot ? rot : "")
      + (includeSca ? sca : "");
    }
    public static string t(Vector3 t) // not to separate on csv
    {
      return $"{t.x} {t.y} {t.z}";
    }
  }

  // データをまとめたリスト
  // 毎フレーム取るか試行ごとに取るかは実験により変わる
  public List<Data> DataList = new List<Data>();

  // ファイル名
  // 日付時刻で決める等する
  private string FileName
  {
    get
    {
      var time = DateTime.Now;
      return $"logPerFrame_{time.Year}_{time.Month}_{time.Day}_{time.Hour}_{time.Minute}_{time.Second}.csv"; // お好みで
    }
  }

  // CSVファイルを生成してデータを出力する
  public void Export(string folder)
  {
    // FileNameの名前でCSVファイルを生成する
    var file = new StreamWriter(Path.Combine(folder, FileName), false, Encoding.GetEncoding("UTF-8"));

    // 1行目：
    file.Write(Data.Header); // file.writeは適当なcsv書き込み用関数だと思ってください

    // 2行目以降：
    foreach (Data data in DataList)
    {
      file.Write("\n" + data.ToString());
    }
  }

  private void OnDestroy()
  {
    var folder = Application.persistentDataPath;
    Directory.CreateDirectory(folder);
    LoggerPerFrame.Instance.Export(folder);
  }
}