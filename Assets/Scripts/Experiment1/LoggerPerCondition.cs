using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class LoggerPerCondition : SingletonMonoBehaviour<LoggerPerFrame> // シングルトンは調べてください
{
  // データ保持用構造体
  public struct Data
  {
    // 参加者ID
    public int id;

    // 実験条件
    // enumで管理すると良い
    public int condition;

    // データ（お好み）
    public float hoge;
    public float fuga;
    public float piyo;

    // コンストラクタ
    // public Data(int id, int condition, float hoge, float fuga, float piyo)
    // {
    //   this.id = id;
    //   this.condition = condition;
    //   this.hoge = ...
		// }

    // CSVの1行目用
    public static string Header
    {
      get
      {
        return "ID, Condition, Hoge, Fuga, Piyo ...";
      }
    }

    // stringへのキャストをオーバーロード
    // CSVの2行目以降を構成するように各要素をコンマで繋げる
    public override string ToString()
    {
      return $"{id},{condition},{hoge},{fuga},{piyo}, ...";
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
      return $"{time.Year}_{time.Month}_{time.Date}_{time.Hour}_{time.Minute}_{time.Second}.csv"; // お好みで
    }
  }

  // CSVファイルを生成してデータを出力する
  public void Export()
  {
    // FileNameの名前でCSVファイルを生成する
    var file = new StreamWriter(FileName, false, Encoding.GetEncoding("UTF-8"));

    // 1行目：
    file.Write(Data.Header); // file.writeは適当なcsv書き込み用関数だと思ってください

    // 2行目以降：
    foreach (Data data in DataList)
    {
      file.Write(data.ToString());
    }
  }
}