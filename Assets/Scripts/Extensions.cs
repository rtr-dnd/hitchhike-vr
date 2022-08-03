using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RootScript
{
  static class Extensions
  {
    public static GameObject GetChildWithName(this GameObject obj, string name) => obj.transform.Find(name)?.gameObject;
  }
}