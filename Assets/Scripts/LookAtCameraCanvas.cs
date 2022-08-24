using UnityEngine;

/// <summary>
/// 常にカメラの方を見るキャンバス
/// </summary>
public class LookAtCameraCanvas : MonoBehaviour
{

  [SerializeField]
  private Transform _camera = null;

  private void Update()
  {
    transform.LookAt(_camera);
  }

}