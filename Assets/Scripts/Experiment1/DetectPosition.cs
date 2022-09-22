using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPosition : MonoBehaviour
{
  public Collider correspondingCollider;
  public string tagName;
  public GameObject visualizer;
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.tag == tagName) visualizer.GetComponent<MeshRenderer>().material.color = Color.blue;
  }
  private void OnTriggerExit(Collider other)
  {
    if (other.gameObject.tag == tagName) visualizer.GetComponent<MeshRenderer>().material.color = Color.white;
  }
}
