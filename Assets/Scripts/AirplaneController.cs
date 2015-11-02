using UnityEngine;
using System.Collections;

//NOTE
//Not a true airplane controller, this is just a chopped up version of the car controller to create an airplane that moves around a runway



public class AirplaneController : MonoBehaviour 
{
	
	public float angleOffset;
	public float breakingDistance = 6f;
 	public float forwardOffset;
	
	public float topSpeed = 150;
	public float maxReverseSpeed = -50;
	public float maxBrakeTorque = 100;
	public float maxTurnAngle = 10;
	public float maxTorque = 10;
	public float decelerationTorque = 30;
	public Vector3 centerOfMassAdjustment = new Vector3(0f,-0.9f,0f);
	public float spoilerRatio = 0.1f;
	public float handbrakeForwardSlip = 0.04f;
	public float handbrakeSidewaysSlip = 0.08f;
	public WheelCollider wheelFL;
	public WheelCollider wheelFR;
	public WheelCollider wheelBack;
	public Transform wheelTransformFL;
	public Transform wheelTransformFR;
	public Transform wheelTransformBack;
	
	private bool applyHandbrake = false;
	
	
	public Transform waypointContainer;
	private float currentSpeed;
	private Transform[] waypoints;
	private int currentWaypoint=0;
	private float inputSteer;
	private float inputTorque;
	
	void Start()
	{	
		//lower center of mass for roll-over resistance
		rigidbody.centerOfMass += centerOfMassAdjustment;
		
		//get the waypoints from the track.
		GetWaypoints();
	}
	
	void SetSlipValues(float forward, float sideways)
	{
		WheelFrictionCurve tempStruct = wheelFR.forwardFriction;
		tempStruct.stiffness = forward;
		wheelFR.forwardFriction = tempStruct;
		tempStruct = wheelFR.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelFR.sidewaysFriction = tempStruct;
		
		tempStruct = wheelFL.forwardFriction;
		tempStruct.stiffness = forward;
		wheelFL.forwardFriction = tempStruct;
		tempStruct = wheelFL.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelFL.sidewaysFriction = tempStruct;
	}
	
	// FixedUpdate is called once per physics frame
	void FixedUpdate () 
	{
		//calculate turn angle
		Vector3 RelativeWaypointPosition = transform.InverseTransformPoint(new Vector3( waypoints[currentWaypoint].position.x, transform.position.y, waypoints[currentWaypoint].position.z ) );
		inputSteer = RelativeWaypointPosition.x / RelativeWaypointPosition.magnitude;
		
		//Spoilers add down pressure based on the car’s speed. (Upside-down lift)
		Vector3 localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
		rigidbody.AddForce(-transform.up * (localVelocity.z * spoilerRatio),ForceMode.Impulse);
		
		//calculate torque.		
		if ( Mathf.Abs( inputSteer ) < 0.5f ) 
		{
			//when making minot turning adjustments speed is based on how far to the next point.
			inputTorque = (RelativeWaypointPosition.z / RelativeWaypointPosition.magnitude);
			applyHandbrake = false;	
		}
		else
		{
			//we need to make a hard turn, if moving fast apply handbrake to slide.
			if(rigidbody.velocity.magnitude > 10)
			{
				applyHandbrake = true;
			}
			//if not moving forward backup and turn opposite.
			else if(localVelocity.z < 0)
			{
				applyHandbrake = false;
				inputTorque = -1;
				inputSteer *= -1;
			}
			//let off the gas while making a hard turn.
			else
			{
				applyHandbrake = false;
				inputTorque = 0;
			}
		}

		
		//if close enough, change waypoints.
		if ( RelativeWaypointPosition.magnitude < 10) 
		{
 			currentWaypoint ++;
			
			//completed a lap
		    if ( currentWaypoint >= waypoints.Length ) 
		    {
		      currentWaypoint = 0;
		    }

		}
		
		//front wheel steering
		inputSteer += AngleRayCasts();
		wheelFL.steerAngle = inputSteer * maxTurnAngle;
		wheelFR.steerAngle = inputSteer * maxTurnAngle;
		
		//calculate max speed in KM/H (optimized calc)
		currentSpeed = wheelFR.radius*wheelFR.rpm*Mathf.PI*0.12f;
		if(currentSpeed < topSpeed && currentSpeed > maxReverseSpeed)
		{
			//check for cars infront
      		float adjustment = ForwardRayCast();
			
			//front wheel drive.
			wheelFL.motorTorque = adjustment * inputTorque * maxTorque;
			wheelFR.motorTorque = adjustment * inputTorque * maxTorque;
		}
		else
		{
			//can't go faster, already at top speed that engine produces.
			wheelFL.motorTorque = 0;
			wheelFR.motorTorque = 0;
		}
	}
	
	void Update()
	{
		//rotate the wheels based on RPM
		float rotationThisFrame = 360*Time.deltaTime;
		wheelTransformFL.Rotate(0,wheelFL.rpm/rotationThisFrame,0);
		wheelTransformFR.Rotate(0,wheelFR.rpm/rotationThisFrame,0);
		wheelTransformBack.Rotate(0,wheelBack.rpm/rotationThisFrame,0);
		
		//turn the wheels according to steering. But make sure you take into account the rotation being applied above.
		wheelTransformFL.localEulerAngles = new Vector3(wheelTransformFL.localEulerAngles.x, wheelFL.steerAngle - wheelTransformFL.localEulerAngles.z, wheelTransformFL.localEulerAngles.z);
		wheelTransformFR.localEulerAngles = new Vector3(wheelTransformFR.localEulerAngles.x, wheelFR.steerAngle - wheelTransformFR.localEulerAngles.z, wheelTransformFR.localEulerAngles.z);
		
	}
	

	void GetWaypoints()
	{
		//NOTE: Unity named this function poorly it also returns the parent’s component.
		Transform[] potentialWaypoints = waypointContainer.GetComponentsInChildren<Transform>();
		
		//initialize the waypoints array so that is has enough space to store the nodes.
		waypoints = new Transform[ (potentialWaypoints.Length - 1) ];
		
		//loop through the list and copy the nodes into the array.
    	//start at 1 instead of 0 to skip the WaypointContainer’s transform.
		for (int i = 1; i < potentialWaypoints.Length; ++i ) 
		{
 			waypoints[ i-1 ] = potentialWaypoints[i];
		}
	}
	
	private float ForwardRayCast()
  	{
	    RaycastHit hit;
	    Vector3 carFront = transform.position + (transform.forward * forwardOffset);
	    //Debug.DrawRay(carFront, transform.forward * breakingDistance);
	
	    //if we detect a car infront of us, slow down or even reverse based on distance.
	    if(Physics.Raycast(carFront, transform.forward, out hit, breakingDistance))
	    {
	      return (((carFront - hit.point).magnitude / breakingDistance) * 2 ) - 1;
	    }
		 	
	    //otherwise no change
	    return 1f;
  	}
	
	private float AngleRayCasts()
	{
		RaycastHit hit;
	    Vector3 carLeft = transform.position + (-transform.right + transform.forward * angleOffset);
		Vector3 carRight = transform.position + (transform.right + transform.forward * angleOffset);
	    //Debug.DrawRay(carLeft, (-transform.right + transform.forward) * breakingDistance);
		//Debug.DrawRay(carRight, (transform.right + transform.forward) * breakingDistance);
		
		float ret = 0f;
	
		//Left Side angle
	    if(Physics.Raycast(carLeft, -transform.right + transform.forward, out hit, breakingDistance))
	    {
	      	ret = 1 - ((carLeft - hit.point).magnitude / breakingDistance);
	    }
		
		//Right Side angle
		if(Physics.Raycast(carRight,transform.right + transform.forward ,out hit,breakingDistance))
		{
			ret =( ( (carRight - hit.point).magnitude / breakingDistance)) - 1;
		} 
		return ret;
	}
	
	public Transform GetCurrentWaypoint()
	{
		return waypoints[currentWaypoint];	
	}
	
	public Transform GetLastWaypoint()
	{
		if(currentWaypoint - 1 < 0)
		{
			return waypoints[waypoints.Length - 1];
		}
		
		return waypoints[currentWaypoint - 1];
	}
	
	
	public float GetCurrentSpeed()
	{
		return wheelFL.radius*wheelFL.rpm*Mathf.PI*0.12f;
	}
	
	public float GetTopSpeed()
	{
		return topSpeed;
	}
	
}
