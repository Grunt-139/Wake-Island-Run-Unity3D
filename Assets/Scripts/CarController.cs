using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour 
{
	public float breakingDistance = 6f;
 	public float forwardOffset;
	public float sideOffset;
	public float angleOffset;


	public int numberOfGears;
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
	public WheelCollider wheelBL;
	public WheelCollider wheelBR;
	public Transform wheelTransformFL;
	public Transform wheelTransformFR;
	public Transform wheelTransformBL;
	public Transform wheelTransformBR;
	public GameObject leftBrakeLight;
	public GameObject rightBrakeLight;
	public Texture2D idleLightTex;
	public Texture2D brakeLightTex;
	public Texture2D reverseLightTex;
	
	private Transform waypointContainer;
	private bool applyHandbrake=false;
	private float currentSpeed;
	private float gearSpread;
	private Transform[] waypoints;
	private int currentWaypoint=0;
	private float inputSteer;
	private float inputTorque;
	
	
	
	//Timer stuff
	private bool runTimer = false;
	private float timer = 0;
	
	void Start()
	{
		//calculate the spread of top speed over the number of gears.
		gearSpread = topSpeed / numberOfGears;
		
		//lower center of mass for roll-over resistance
		rigidbody.centerOfMass += centerOfMassAdjustment;
		
		
		//Get random waypoint container
		waypointContainer = RaceManager.Instance.GetRandomWaypointContainer();
		//get the waypoints from the track.
		GetWaypoints();
	}
	
	void SetSlipValues(float forward, float sideways)
	{
		WheelFrictionCurve tempStruct = wheelBR.forwardFriction;
		tempStruct.stiffness = forward;
		wheelBR.forwardFriction = tempStruct;
		tempStruct = wheelBR.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelBR.sidewaysFriction = tempStruct;
		
		tempStruct = wheelBL.forwardFriction;
		tempStruct.stiffness = forward;
		wheelBL.forwardFriction = tempStruct;
		tempStruct = wheelBL.sidewaysFriction;
		tempStruct.stiffness = sideways;
		wheelBL.sidewaysFriction = tempStruct;
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
		if ( RelativeWaypointPosition.magnitude < 25 ) 
		{
 			currentWaypoint ++;
			
			//completed a lap
		    if ( currentWaypoint >= waypoints.Length ) 
		    {
		      currentWaypoint = 0;
		      RaceManager.Instance.LapFinishedByAI(this);
		    }

		}
		
		//front wheel steering
		inputSteer += SideRayCast() + AngleRayCasts();
		wheelFL.steerAngle = inputSteer * maxTurnAngle;
		wheelFR.steerAngle = inputSteer * maxTurnAngle;
		
		//calculate max speed in KM/H (optimized calc)
		currentSpeed = wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
		if(currentSpeed <= topSpeed && currentSpeed >= maxReverseSpeed)
		{
			
		    //check for cars infront
      		float adjustment = ForwardRayCast();

			//rear wheel drive.
			wheelBL.motorTorque = adjustment * inputTorque * maxTorque;
			wheelBR.motorTorque = adjustment * inputTorque * maxTorque;
		}
		else
		{
			//can't go faster, already at top speed that engine produces.
			wheelBL.motorTorque = 0;
			wheelBR.motorTorque = 0;
		}
	}
	
	void UpdateWheelPositions()
	{
		//move wheels based on their suspension.
		WheelHit contact = new WheelHit();
		if(wheelFL.GetGroundHit(out contact))
		{
			Vector3 temp = wheelFL.transform.position;
			temp.y = (contact.point + (wheelFL.transform.up*wheelFL.radius)).y;
			wheelTransformFL.position = temp;
		}
		if(wheelFR.GetGroundHit(out contact))
		{
			Vector3 temp = wheelFR.transform.position;
			temp.y = (contact.point + (wheelFR.transform.up*wheelFR.radius)).y;
			wheelTransformFR.position = temp;
		}
		if(wheelBL.GetGroundHit(out contact))
		{
			Vector3 temp = wheelBL.transform.position;
			temp.y = (contact.point + (wheelBL.transform.up*wheelBL.radius)).y;
			wheelTransformBL.position = temp;
		}
		if(wheelBR.GetGroundHit(out contact))
		{
			Vector3 temp = wheelBR.transform.position;
			temp.y = (contact.point + (wheelBR.transform.up*wheelBR.radius)).y;
			wheelTransformBR.position = temp;
		}
	}
	
	void Update()
	{
		if(runTimer)
		{
			timer += Time.deltaTime;
		}
		//rotate the wheels based on RPM
		float rotationThisFrame = 360*Time.deltaTime;
		wheelTransformFL.Rotate(0,wheelFL.rpm/rotationThisFrame,0);
		wheelTransformFR.Rotate(0,wheelFR.rpm/rotationThisFrame,0);
		wheelTransformBL.Rotate(0,wheelBL.rpm/rotationThisFrame,0);
		wheelTransformBR.Rotate(0,wheelBR.rpm/rotationThisFrame,0);
		
		//turn the wheels according to steering. But make sure you take into account the rotation being applied above.
		wheelTransformFL.localEulerAngles = new Vector3(wheelTransformFL.localEulerAngles.x, wheelFL.steerAngle - wheelTransformFL.localEulerAngles.z, wheelTransformFL.localEulerAngles.z);
		wheelTransformFR.localEulerAngles = new Vector3(wheelTransformFR.localEulerAngles.x, wheelFR.steerAngle - wheelTransformFR.localEulerAngles.z, wheelTransformFR.localEulerAngles.z);
		
		//Adjust the wheels heights based on the suspension.
		UpdateWheelPositions();
		
		//Determine what texture to use on our brake lights right now.
		DetermineBreakLightState();
		
		//adjust engine sound
		EngineSound();
	}
	
	void DetermineBreakLightState()
	{
		if((currentSpeed > 0 && inputTorque < 0) 
			|| (currentSpeed < 0 && inputTorque > 0)
			|| applyHandbrake)
		{
			leftBrakeLight.renderer.material.mainTexture = brakeLightTex;
			Light leftLight = leftBrakeLight.GetComponentInChildren<Light>();
			leftLight.color = Color.red;
			leftLight.intensity = 1;
			rightBrakeLight.renderer.material.mainTexture = brakeLightTex;
			Light rightLight = rightBrakeLight.GetComponentInChildren<Light>();
			rightLight.color = Color.red;
			rightLight.intensity = 1;
		}
		else if(currentSpeed < 0 && inputTorque < 0)
		{
			leftBrakeLight.renderer.material.mainTexture = reverseLightTex;
			Light leftLight = leftBrakeLight.GetComponentInChildren<Light>();
			leftLight.color = Color.white;
			leftLight.intensity = 1;
			rightBrakeLight.renderer.material.mainTexture = reverseLightTex;
			Light rightLight = rightBrakeLight.GetComponentInChildren<Light>();
			rightLight.color = Color.white;
			rightLight.intensity = 1;
		}
		else
		{
			leftBrakeLight.renderer.material.mainTexture = idleLightTex;
			Light leftLight = leftBrakeLight.GetComponentInChildren<Light>();
			leftLight.color = Color.white;
			leftLight.intensity = 0;
			rightBrakeLight.renderer.material.mainTexture = idleLightTex;
			Light rightLight = rightBrakeLight.GetComponentInChildren<Light>();
			rightLight.color = Color.white;
			rightLight.intensity = 0;
			
		}
	}
	
	void EngineSound()
	{
		//going forward calculate how far along that gear we are and the pitch sound.
		if(currentSpeed > 0)
		{
			if(currentSpeed > topSpeed)
			{
				audio.pitch = 1.75f;
			}
			else
			{
				audio.pitch = ((currentSpeed % gearSpread) / gearSpread) + 0.75f;
			}
		}
		//when reversing we have only one gear.
		else
		{
			audio.pitch = (currentSpeed / maxReverseSpeed) + 0.75f;
		}
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
	
	private float SideRayCast()
	{
		RaycastHit hit;
	    Vector3 carLeft = transform.position + (-transform.right * sideOffset);
		Vector3 carRight = transform.position + (transform.right * sideOffset);
	    //Debug.DrawRay(carLeft, -transform.right * breakingDistance);
		//Debug.DrawRay(carRight, transform.right * breakingDistance);
		
		float ret = 0f;
	
		//Left Side
	    if(Physics.Raycast(carLeft, -transform.right, out hit, breakingDistance))
	    {
	      	ret = 1 - ((carLeft - hit.point).magnitude / breakingDistance);
	    }
		
		//Right Side
		if(Physics.Raycast(carRight,transform.right,out hit,breakingDistance))
		{
			ret =( ( (carRight - hit.point).magnitude / breakingDistance)) - 1;
		}
		return ret;
		//return 0f;
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
	
	public float GetTime()
	{
		return timer;
	}
	
	public void StartTimer()
	{
		runTimer = true;
	}
	
	public void StopTimer()
	{
		runTimer = false;
	}
	
	
	public float GetCurrentSpeed()
	{
		return wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
	}
	
	public float GetTopSpeed()
	{
		return topSpeed;
	}
	
}
