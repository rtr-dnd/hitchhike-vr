using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static Experiment1Location;

public class LoggerPerCondition : SingletonMonoBehaviour<LoggerPerCondition> // シングルトンは調べてください
{
  // データ保持用構造体
  public struct Data
  {
    public double time;
    public ExperimentMode mode;
    public int trialNum;
    public float reachingTime;
    public float placingTime;
    public int currentObjectIndex;
    public int currentTargetIndex;
    public Vector3 currentTargetLocation;

    // コンストラクタ
    public Data(
      double a_time,
      ExperimentMode a_mode,
      int a_trialNum,
      float a_reachingTime,
      float a_placingTime,
      int a_currentObjectIndex,
      int a_currentTargetIndex,
      Vector3 a_currentTargetLocation
    )
    {
      this.time = a_time;
      this.mode = a_mode;
      this.trialNum = a_trialNum;
      this.reachingTime = a_reachingTime;
      this.placingTime = a_placingTime;
      this.currentObjectIndex = a_currentObjectIndex;
      this.currentTargetIndex = a_currentTargetIndex;
      this.currentTargetLocation = a_currentTargetLocation;
    }

    // CSVの1行目用
    public static string Header
    {
      get
      {
        return $@"
time,
mode,
trialNum,
reachingTime,
placingTime,
currentObjectIndex,
currentTargetIndex,
currentTargetLocation,".Replace(Environment.NewLine, "");
      }
    }

    // stringへのキャストをオーバーロード
    // CSVの2行目以降を構成するように各要素をコンマで繋げる
    public override string ToString()
    {
      return $@"
{time},
{mode},
{trialNum},
{reachingTime},
{placingTime},
{currentObjectIndex},
{currentTargetIndex},
{t(currentTargetLocation)},".Replace(Environment.NewLine, "");
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
      return $"logPerCondition_{time.Year}_{time.Month}_{time.Day}_{time.Hour}_{time.Minute}_{time.Second}.csv"; // お好みで
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
    file.Close();
  }

  private void OnDestroy()
  {
    var folder = Application.persistentDataPath;
    Directory.CreateDirectory(folder);
    LoggerPerCondition.Instance.Export(folder);
    Debug.Log("exported log per condition");
  }
}