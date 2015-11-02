using UnityEngine;
using System.Collections;

public class WaypointGates : MonoBehaviour {
	
	public Car playerCar;
	public float facingSpeed = 1f;
	public float heightOffset = 10f;
	
	private Vector3 lookDirection;
	private Quaternion lookRotation;
	
	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.RotateAround(transform.position,Vector3.up,Vector3.Angle(playerCar.transform.position, transform.position) );
	}
	
	void OnTriggerEnter(Collider other)
	{
		if(other.rigidbody == playerCar.rigidbody)
		{
			playerCar.IncreaseWaypoint();
			RaceManager.Instance.UpdatePlayerMarker();
		}
	}
	
	public float GetHeightOffset()
	{
		return heightOffset;
	}
}
