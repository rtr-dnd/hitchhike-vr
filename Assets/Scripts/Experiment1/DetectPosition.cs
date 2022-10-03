using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPosition : MonoBehaviour
{
  // public int correspondingColliderId;
  public Collider correspondingCollider;
  public string tagName;
  public GameObject visualizer;
  [SerializeField] float threshold = 0.1f;
  bool isOK = false;
  public bool GetOK() { return isOK; }

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    // var correspondingCollider = Exper
    // var distance = Vector3.Distance(gameObject.transform.position, correspondingCollider.transform.position);
    // Debug.Log(distance);
    // if (distance < threshold)
    // {
    //   isOK = true;
    //   visualizer.GetComponent<MeshRenderer>().material.color = Color.blue;
    // }
    // else
    // {
    //   isOK = false;
    //   visualizer.GetComponent<MeshRenderer>().material.color = Color.white;
    // }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.tag == tagName)
    {
      isOK = true;
      visualizer.GetComponent<MeshRenderer>().material.color = Color.blue;
    }
  }
  private void OnTriggerExit(Collider other)
  {
    if (other.gameObject.tag == tagName)
    {
      isOK = false;
      visualizer.GetComponent<MeshRenderer>().material.color = Color.white;
    }
  }
}
