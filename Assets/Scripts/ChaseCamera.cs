using UnityEngine;
using System.Collections;

public class ChaseCamera : MonoBehaviour {
	
	public Transform car;
	public float distance;
	public float height;
	public float rotationDamping = 3f;
	public float heightDamping = 2f;
	public float fovDamping = 2f;
	
	private float desiredAngle=0;
	private Car carComponent;
	
	// Use this for initialization
	void Start () 
	{
		carComponent = car.GetComponent<Car>();
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	void FixedUpdate()
	{
		desiredAngle = car.eulerAngles.y; // NOTE: Removed from LateUpdate() and added here.
		//If the car is going backwards add 180 to the wanted rotation
		Vector3 localVelocity = car.InverseTransformDirection(car.rigidbody.velocity);
		if(localVelocity.z < -0.5f)
		{
			desiredAngle += 180;
		}
		
		
		//Update my field of view
		if(carComponent.GetCurrentSpeed() > carComponent.GetTopSpeed() *0.75f)
		{
			float currentFOV = camera.fieldOfView;
			float desiredFOV = 90f;
			currentFOV = Mathf.Lerp(currentFOV,desiredFOV,fovDamping * Time.deltaTime);
			camera.fieldOfView = currentFOV;
		}
		else
		{
			float currentFOV = camera.fieldOfView;
			float desiredFOV = 60f;
			currentFOV = Mathf.Lerp(currentFOV,desiredFOV,fovDamping * Time.deltaTime);
			camera.fieldOfView = currentFOV;
		}
		
		
	}
	
	//LateUpdate is called once per frame after Update() has been called
	void LateUpdate()
	{
		float currentAngle = transform.eulerAngles.y;
		float currentHeight = transform.position.y;
		float desiredHeight = car.position.y + height;
		//Now move towards our goals
		currentAngle = Mathf.LerpAngle(currentAngle,desiredAngle, rotationDamping * Time.deltaTime);
		currentHeight = Mathf.Lerp(currentHeight, desiredHeight, heightDamping * Time.deltaTime);
		Quaternion currentRotation = Quaternion.Euler(0,currentAngle,0);
		//Set our new positions
		Vector3 finalPosition = car.position - (currentRotation * Vector3.forward * distance);
		finalPosition.y = currentHeight;
		transform.position = finalPosition;
		transform.LookAt(car);
	}
}
