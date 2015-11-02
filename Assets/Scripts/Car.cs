using UnityEngine;
using System.Collections;

public class Car : MonoBehaviour 
{
	//Handbrake
	public float maxBrakeTorque = 100;
	public float handbrakeForwardSlip = 0.04f;
 	public float handbrakeSidewaysSlip = 0.08f;
	private bool applyHandbrake = false;
	
	//Speed, torque info
	public float topSpeed = 150;
	public float topReverseSpeed = 100;
	public float maxTurnAngle = 10;
	public float maxTorque = 10;
	public float decelerationTorque = 30;
	//Center of mass adjustment
	public Vector3 centerOfMassAdjustment = new Vector3(0f,-0.9f,0f);
	public float spoilerRatio = 0.1f;
	//Wheel colliders
	public WheelCollider wheelFL;
	public WheelCollider wheelFR;
	public WheelCollider wheelBL;
	public WheelCollider wheelBR;
	//Wheel Transforms
	public Transform wheelTransformFL;
	public Transform wheelTransformFR;
	public Transform wheelTransformBL;
	public Transform wheelTransformBR;
	
	//Brake lights
	public GameObject leftBrakeLight;
	public GameObject rightBrakeLight;
	public Texture2D idleLightTex;
	public Texture2D brakeLightTex;
	public Texture2D reverseLightTex;
	
	//Physical Light objects
	public Light leftBrakeSpotlight;
	public Light rightBrakeSpotlight;
	
	//Current Speed
	private float currentSpeed;
	
	//Gears
	public int numberOfGears;
	private float gearSpread;
	
	//Particle Effects
	public ParticleSystem exhaust;
	public ParticleSystem dirt;
	
	//Speedometer
	public Texture2D speedometer;
  	public Texture2D needle;
	
	//Boost stuff
	public float boostDelay = 1f;
	public float boostBonus = 10f;
	public float boostTime = 0.5f;
	private float curBoostTime;  
	private bool isBoosting = false;
	
	//Waypoint stuff
	public Transform waypointContainer;
	private Transform[] waypoints;
	private int currentWaypoint=0;
	
	//Can the player drive
	private bool engineKilled = true;
	
	//Timer stuff
	private bool runTimer = false;
	private float timer = 0;
	
	
	void Start()
	{
		//lower center of mass for roll-over resistance
		rigidbody.centerOfMass += centerOfMassAdjustment;
		
		//calculate the spread of top speed over the number of gears.
		gearSpread = topSpeed / numberOfGears; 
		
		dirt.enableEmission = false;
		
		curBoostTime = boostDelay;
		
		//Adjust the wheels heights based on the suspension
		UpdateWheelPositions();
		
		//get the waypoints from the track.
		GetWaypoints();
	}
	
	// FixedUpdate is called once per physics frame
	void FixedUpdate () 
	{
		if(!engineKilled)
		{
			
//			if(curBoostTime < 0 && !isBoosting)
//			{
//				if(Input.GetKeyUp(KeyCode.LeftShift))
//				{
//					isBoosting = true;
//					curBoostTime = boostTime;
//				}
//			}
//			else if(curBoostTime < 0 && isBoosting)
//			{
//				if(curBoostTime <=0)
//				{
//					isBoosting = false;
//					curBoostTime = boostDelay;
//				}
//			}
			
			curBoostTime -= Time.deltaTime;
			
			float curTorque = maxTorque;
			
			if(isBoosting)
			{
				curTorque +=boostBonus;
			}
			
			//calculate max speed in KM/H (optimized calc)
			currentSpeed = wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
			if(currentSpeed < topSpeed && currentSpeed > -topReverseSpeed)
			{
				//rear wheel drive.
				wheelBL.motorTorque = Input.GetAxis("Vertical") * curTorque;
				wheelBR.motorTorque = Input.GetAxis("Vertical") * curTorque;
			}
			else
			{
				//can't go faster, already at top speed that engine produces.
				wheelBL.motorTorque = 0;
				wheelBR.motorTorque = 0;
			}
			
			if(currentSpeed > topSpeed *0.5f)
			{
				dirt.enableEmission = true;
			}
			else
			{
				dirt.enableEmission = false;
			}
			
		//	print("Speed " + currentSpeed + " Velocity" + rigidbody.velocity + " Magnitude " + rigidbody.velocity.magnitude);
			
			//Spoilers add down pressure based on the car’s speed. (Upside-down lift)
			Vector3 localVelocity = transform.InverseTransformDirection(rigidbody.velocity);
			rigidbody.AddForce(-transform.up * (localVelocity.z * spoilerRatio),ForceMode.Impulse);
			
			//front wheel steering
			wheelFL.steerAngle = Input.GetAxis("Horizontal") * maxTurnAngle;
			wheelFR.steerAngle = Input.GetAxis("Horizontal")* maxTurnAngle;
			
			
			//Handbrake controls
		    if(Input.GetButton("Jump"))
		    {
		      applyHandbrake = true;
		      wheelFL.brakeTorque = maxBrakeTorque;
		      wheelFR.brakeTorque = maxBrakeTorque;
			//Wheels are locked, so power slide!
		      if(rigidbody.velocity.magnitude > 1)
		      {
		        SetSlipValues(handbrakeForwardSlip, handbrakeSidewaysSlip);
		      }
		      else //skid to a stop, regular friction enabled.
		      {
			      SetSlipValues(1f,1f);
	      	  }
		    }
		    else
		    {
		      applyHandbrake = false;
		      wheelFL.brakeTorque = 0;
		      wheelFR.brakeTorque = 0;
			  SetSlipValues(1f,1f);
	
		    }
	
			//apply deceleration when not pressing the gas or when breaking in either direction.
			if( !applyHandbrake && ((Input.GetAxis("Vertical") <= -0.5f && localVelocity.z > 0 ) || (Input.GetAxis("Vertical") >= 0.5f && localVelocity.z < 0) ))
		    {
		      wheelBL.brakeTorque = decelerationTorque + curTorque;
		      wheelBR.brakeTorque = decelerationTorque + curTorque;
		    }
		    else if(!applyHandbrake && Input.GetAxis("Vertical") == 0)
		    {
		       wheelBL.brakeTorque = decelerationTorque;
		       wheelBR.brakeTorque = decelerationTorque;
		    }
		    else
		    {
		      wheelBL.brakeTorque = 0;
		      wheelBR.brakeTorque = 0;
			}
			
			
		    //Determine what texture to use on our brake lights right now.
	    	DetermineBreakLightState();
		}
		//Adjust engine sound
		EngineSound();
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
		
//		if(wheelTransformFL.rotation.y < 10 && wheelTransformFL.rotation.y > -10)
//		{
//			Vector3 rot = new Vector3(wheelTransformFL.localEulerAngles.x,Input.GetAxis("Horizontal") * maxTurnAngle - wheelTransformFL.localEulerAngles.y,wheelTransformFL.localEulerAngles.z);
//			wheelTransformFL.localEulerAngles = rot;
//			wheelTransformFR.localEulerAngles = rot;
//		}
		
		//move wheels based on their suspension.
		WheelHit contact = new WheelHit();
		if(wheelFL.GetGroundHit(out contact))
		{
			wheelTransformFL.position = contact.point + (wheelFL.transform.up*wheelFL.radius);
		}
		if(wheelFR.GetGroundHit(out contact))
		{
			wheelTransformFR.position = contact.point + (wheelFR.transform.up*wheelFR.radius);
		}
		if(wheelBL.GetGroundHit(out contact))
		{
			wheelTransformBL.position = contact.point + (wheelBL.transform.up*wheelBL.radius);
		}
		if(wheelBR.GetGroundHit(out contact))
		{
			wheelTransformBR.position = contact.point + (wheelBR.transform.up*wheelBR.radius);
		}
	}
	
	void SetSlipValues(float forward, float sideways)
	{
		//Change the stiffness values of wheel friction curve and then reapply it.
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
	
	
	void DetermineBreakLightState()
	{
		if((currentSpeed > 0 && Input.GetAxis("Vertical") < 0) 
		|| (currentSpeed < 0 && Input.GetAxis("Vertical") > 0)
		|| applyHandbrake)
		{
		    leftBrakeLight.renderer.material.mainTexture = brakeLightTex;
		    rightBrakeLight.renderer.material.mainTexture = brakeLightTex;
			
			leftBrakeSpotlight.enabled = true;
			rightBrakeSpotlight.enabled = true;
			
			leftBrakeSpotlight.color = Color.red;
			rightBrakeSpotlight.color = Color.red;
			 
		}
		else if(currentSpeed < 0 && Input.GetAxis("Vertical") < 0)
		{
		    leftBrakeLight.renderer.material.mainTexture = reverseLightTex;
		    rightBrakeLight.renderer.material.mainTexture = reverseLightTex;
			
			leftBrakeSpotlight.enabled = true;
			rightBrakeSpotlight.enabled = true;
			
			leftBrakeSpotlight.color = Color.white;
			rightBrakeSpotlight.color = Color.white;
		}
		else
	  	{
		    leftBrakeLight.renderer.material.mainTexture = idleLightTex;
		 	rightBrakeLight.renderer.material.mainTexture = idleLightTex;
			
			leftBrakeSpotlight.enabled = false;
			rightBrakeSpotlight.enabled = false;
			
			
		}
	}
	
	void EngineSound()
	{
		//Going forward calculate how far along that gear we are and the pitch sound.
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
	}
	
	//Move wheels based on their suspension
	void UpdateWheelPositions()
	{
		WheelHit contact = new WheelHit();
		
		if(wheelFL.GetGroundHit(out contact))
		{
			Vector3 temp = wheelFL.transform.position;
			temp.y = (contact.point + (wheelFL.transform.up * wheelFL.radius)).y;
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
	
	public void IncreaseWaypoint()
	{
		currentWaypoint++;
		
			
		//completed a lap
	    if ( currentWaypoint >= waypoints.Length ) 
	    {
	      currentWaypoint = 0;
	      RaceManager.Instance.LapFinishedByPlayer();
	    }
		RaceManager.Instance.UpdatePlayerMarker();
	}
	
	
	public float GetCurrentSpeed()
	{
		return wheelBL.radius*wheelBL.rpm*Mathf.PI*0.12f;
	}
	
	public float GetTopSpeed()
	{
		return topSpeed;
	}
	
	public void KillTheEngine()
	{
		engineKilled = true;
		rigidbody.velocity = Vector3.zero;
	}
	
	public void StartTheEngine()
	{
		engineKilled = false;
	}
	
	public bool IsEngineKilled()
	{
		return engineKilled;
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
	
	void OnGUI()
	{
		if(curBoostTime < 0 && !isBoosting)
		{
		//	GUI.Label(new Rect(Screen.width *0.5f, Screen.height *0.25f, 100f,100f),"Left Shift to Boost");
		}
		//Speedometer
		GUI.DrawTexture(new Rect(Screen.width-300,Screen.height-150,300,150),speedometer);
		float speedFactor=currentSpeed/topSpeed;
		float rotationAngle = Mathf.Lerp(0,180,Mathf.Abs(speedFactor));	
		GUIUtility.RotateAroundPivot(rotationAngle,new Vector2(Screen.width-150,Screen.height));
		GUI.DrawTexture(new Rect(Screen.width - 300, Screen.height - 150, 300, 300),needle);
	}

}

