using UnityEngine;
using System.Collections;
using System.Collections.Generic; 

public class RaceManager : MonoBehaviour 
{
	public Rigidbody[] cars;
	public Rigidbody[] planes;
	public Rigidbody playerCar;
	public float respawnDelay = 5f;
	public float playerRespawnDelay = 5f;
	public float distanceToCover = 1f;
	public Transform playerWaypointMarker;
	private WaypointGates playerWaypointScript;
	public int lapsToWin = 3;
	public Transform[] waypointContainers;

	
	private CarController[] scripts;
	private AirplaneController[] airplaneScripts;
	private Car playerScript;
	private float[] respawnTimes;
	private float[] distanceLeftToTravel;
	private Transform[] waypoint;
	private bool[] finished;
	private int playerIndex;
	
	public float minHeight;
	
	private int[] laps;
	
	public static RaceManager Instance{ get{return instance; } }
	private static RaceManager instance = null;
	
	//Countdown timer
	public Texture2D startRaceImage;
  	public Texture2D digit1Image;
  	public Texture2D digit2Image;
  	public Texture2D digit3Image;
  	private int countdownTimerDelay;
  	private float countdownTimerStartTime;
	
	//Players place
	private int playersPlace = 1;
	
	//Winners
	private List<GameObject> winners;
	//Timers
	private List<float> times;

	void Awake()
	{
		if(instance != null && instance != this)
		{
			Destroy(this.gameObject);
			return;
		}
		else
		{
			instance = this;
		}
		CountdownTimerReset(3);
	}
	
	// Use this for initialization
	void Start () 
	{
		winners = new List<GameObject>();
		
		laps = new int[cars.Length + 1];
		times = new List<float>();
		
		respawnTimes = new float[cars.Length + planes.Length +1];
		distanceLeftToTravel = new float[cars.Length + planes.Length +1];
		scripts = new CarController[cars.Length];
		airplaneScripts = new AirplaneController[planes.Length];
		waypoint = new Transform[cars.Length + planes.Length +1];
		finished = new bool[cars.Length + 1];
		
		//intialize the arrays with starting values
		for(int i=0; i < cars.Length; ++i)
		{
			scripts[i] = cars[i].gameObject.GetComponent<CarController>();
			scripts[i].enabled = false;
			respawnTimes[i] = respawnDelay;
			distanceLeftToTravel[i] = float.MaxValue;
			laps[i] = 0 ;
			finished[i] = false; 
			scripts[i].StartTimer();
		}
		
		//Planes
		//Start at the cars arrays length for the index, then push forward while less then the two added together
		for(int i = cars.Length; i < cars.Length + planes.Length; i++)
		{
			airplaneScripts[i - cars.Length] = planes[i - cars.Length].gameObject.GetComponent<AirplaneController>();
			respawnTimes[i] = respawnDelay;
			distanceLeftToTravel[i] = float.MaxValue;
		}
			
		playerIndex = cars.Length + planes.Length;
		playerScript = playerCar.gameObject.GetComponent<Car>();
		playerScript.KillTheEngine();
		respawnTimes[playerIndex] = playerRespawnDelay;
		laps[playerIndex - planes.Length] = 0;
		distanceLeftToTravel[playerIndex] = float.MaxValue;
		finished[playerIndex - planes.Length] = false; 
		playerScript.StartTimer();
		
		playerWaypointScript = playerWaypointMarker.GetComponent<WaypointGates>();
		UpdatePlayerMarker();
	}
	
	// Update is called once per frame
	void Update () 
	{
		//check if any of the cars need a respawn.
	 	for(int i = 0; i < cars.Length; ++i)
		{
			Transform nextWaypoint = scripts[i].GetCurrentWaypoint();
			float distanceCovered = (nextWaypoint.position - cars[i].position).magnitude;
			
			if(finished[i] == false)
			{
				//if the car has moved far enough or is now moving to a new waypoint reset its values.
				if(distanceLeftToTravel[i] - distanceToCover > distanceCovered || waypoint[i] != nextWaypoint)
				{
					waypoint[i] = nextWaypoint;
					respawnTimes[i] = respawnDelay;
					distanceLeftToTravel[i] = distanceCovered;
				}
				//otherwise tick down time before we respawn it.
				else
				{
					respawnTimes[i] -= Time.deltaTime;
				}
			
				//if it's respawn timer has elapsed OR if it fell below the minimum height 
				if(respawnTimes[i] <= 0 || cars[i].transform.position.y < minHeight)
				{
					//reset its respawn tracking variables
					respawnTimes[i] = respawnDelay;
					distanceLeftToTravel[i] = float.MaxValue;
					cars[i].velocity = Vector3.zero;
					cars[i].angularVelocity = Vector3.zero;
					//And spawn it at its last waypoint facing the next waypoint.
					Transform lastWaypoint = scripts[i].GetLastWaypoint();	
					cars[i].position = lastWaypoint.position;
					cars[i].rotation = Quaternion.LookRotation(nextWaypoint.position - lastWaypoint.position);
				
				}
				
			    if(laps[i] >= lapsToWin)
			    {
			      	finished[i]= true;
				  	scripts[i].enabled = false;
					winners.Add(cars[i].gameObject);
					times.Add(scripts[i].GetTime());
					scripts[i].StopTimer();
			    }
			}
		}
		
		//Plane Checks
		for(int i = cars.Length; i < cars.Length + planes.Length; ++i)
		{
			Transform nextWaypoint = airplaneScripts[i - cars.Length].GetCurrentWaypoint();
			float distanceCovered = (nextWaypoint.position - planes[i - cars.Length].position).magnitude;
			
			//if the car has moved far enough or is now moving to a new waypoint reset its values.
			if(distanceLeftToTravel[i] - distanceToCover > distanceCovered || waypoint[i] != nextWaypoint)
			{
				waypoint[i] = nextWaypoint;
				respawnTimes[i] = respawnDelay;
				distanceLeftToTravel[i] = distanceCovered;
			}
			//otherwise tick down time before we respawn it.
			else
			{
				respawnTimes[i] -= Time.deltaTime;
			}
		
			//if it's respawn timer has elapsed OR if it fell below the minimum height 
			if(respawnTimes[i] <= 0 || planes[i - cars.Length].transform.position.y < minHeight)
			{
				//reset its respawn tracking variables
				respawnTimes[i] = respawnDelay;
				distanceLeftToTravel[i] = float.MaxValue;
				planes[i - cars.Length].velocity = Vector3.zero;
				planes[i - cars.Length].angularVelocity = Vector3.zero;
				//And spawn it at its last waypoint facing the next waypoint.
				Transform lastWaypoint = airplaneScripts[i - cars.Length].GetLastWaypoint();	
				planes[i - cars.Length].position = lastWaypoint.position;
				planes[i - cars.Length].rotation = Quaternion.LookRotation(nextWaypoint.position - lastWaypoint.position);
			
			}
		}
		
		//Player checks
		Transform playerNextWaypoint = playerScript.GetCurrentWaypoint();
		float playerDistanceCovered = (playerNextWaypoint.position - playerCar.position).magnitude;
		
		//Lets check to see if the player can even go
		if(!playerScript.IsEngineKilled())
		{
			//if the car has moved far enough or is now moving to a new waypoint reset its values.
			if(distanceLeftToTravel[playerIndex] - distanceToCover > playerDistanceCovered || waypoint[playerIndex] != playerNextWaypoint)
			{
				waypoint[playerIndex] = playerNextWaypoint;
				respawnTimes[playerIndex] = playerRespawnDelay;
				distanceLeftToTravel[playerIndex] = playerDistanceCovered;
			}
			else
			{
				respawnTimes[playerIndex]-=Time.deltaTime;
			}
		
			//if it's respawn timer has elapsed OR if it fell below the minimum height 
			if(respawnTimes[playerIndex] <= 0 || playerCar.transform.position.y < minHeight)
			{
				//reset its respawn tracking variables
				respawnTimes[playerIndex] = respawnDelay;
				distanceLeftToTravel[playerIndex] = float.MaxValue;
				playerCar.velocity = Vector3.zero;
				playerCar.angularVelocity = Vector3.zero;
				//And spawn it at its last waypoint facing the next waypoint.
				Transform lastWaypoint = playerScript.GetLastWaypoint();	
				playerCar.position = lastWaypoint.position;
				playerCar.rotation = Quaternion.LookRotation(playerNextWaypoint.position - lastWaypoint.position);
			
			}
			
		    if(laps[playerIndex - planes.Length] >= lapsToWin)
		    {
		      	finished[playerIndex - planes.Length] = true;
				playerScript.KillTheEngine();
				winners.Add(playerCar.gameObject);
				playerScript.StopTimer();
				times.Add(playerScript.GetTime());				
		    }	
		
		}

		if(Input.GetKeyUp("f5"))
		{
			//Respawn the player
			//reset its respawn tracking variables
			respawnTimes[playerIndex] = respawnDelay;
			distanceLeftToTravel[playerIndex] = float.MaxValue;
			playerCar.velocity = Vector3.zero;
			playerCar.angularVelocity = Vector3.zero;
			//And spawn it at its last waypoint facing the next waypoint.
			Transform lastWaypoint = playerScript.GetLastWaypoint();	
			playerCar.position = lastWaypoint.position;
			playerCar.rotation = Quaternion.LookRotation(playerNextWaypoint.position - lastWaypoint.position);
		}
		
		
		UpdatePlayersPlace();
	}
	
	void OnGUI()
 	{
    	GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
	    GUILayout.FlexibleSpace();
	    GUILayout.BeginHorizontal();
	    GUILayout.FlexibleSpace();
		
		//Display the countdown as long as the players car is disabled
		if(playerScript.IsEngineKilled() == true)
		{
	    	GUILayout.Label(CountdownTimerImage());	
		}
		
		//GUILayout.Label("Place: " + playersPlace + "/" + (cars.Length + 1));
		
		//If the player has finished their laps
		if(finished[playerIndex - planes.Length] == true)
		{
			GUILayout.BeginArea(new Rect(Screen.width * 0.25f, Screen.height * 0.25f,Screen.width * 0.5f,Screen.height *0.5f));
			for(int i = 0; i < winners.Count; i++)
			{
				GUILayout.Label(i+1 + ": " + winners[i].name + " " + times[i]);
			}
			if(GUILayout.Button("Play again?",GUILayout.Width(Screen.width  * 0.15f)))
			{
				Application.LoadLevel(Application.loadedLevel);
			}
			
			playerCar.velocity = Vector3.zero;
			
			for(int i=0; i < cars.Length; i++)
			{
				cars[i].velocity = Vector3.zero;
			}
			
			if(GUILayout.Button("Menu", GUILayout.Width(Screen.width * 0.15f)))
			{
				Application.LoadLevel(1);
			}
			GUILayout.EndArea();
		}
		
	    GUILayout.FlexibleSpace();
	    GUILayout.EndHorizontal();
	    GUILayout.FlexibleSpace();
	    GUILayout.EndArea();
  	}
  
	Texture2D CountdownTimerImage()
	{
	  switch(CountdownTimerSecondsRemaining())
	  {
	    case 3:
	      return digit3Image;
	    case 2:
	      return digit2Image;
	    case 1:
	      return digit1Image;
	    case 0:
		  StartRace();
	      return startRaceImage;
		default:
			return null;
	  }
	}
	
	public void UpdatePlayerMarker()
	{
		Vector3 temp = playerScript.GetCurrentWaypoint().position;
		temp.y += playerWaypointScript.GetHeightOffset();
		playerWaypointMarker.position = temp;
	}
	
	void StartRace()
	{
		for(int i=0; i < scripts.Length; i++)
		{
			scripts[i].enabled = true;
		}
		playerScript.StartTheEngine();
	}
	
	int CountdownTimerSecondsRemaining()
	{
	  int elapsedSeconds = (int) (Time.time - countdownTimerStartTime);
	  int secondsLeft = (countdownTimerDelay - elapsedSeconds);
	  return secondsLeft;
	}
	  
	void CountdownTimerReset(int delayInSeconds)
	{
	  countdownTimerDelay = delayInSeconds;
	  countdownTimerStartTime = Time.time;
	}	

	public void LapFinishedByAI(CarController car)
	{
		for(int i=0; i < scripts.Length; i++)
		{
			if(scripts[i] == car)
			{
				laps[i]++;
				break;
			}
		}
	}
	
	public void UpdatePlayersPlace()
	{
		//TO-DO
		//Need to compare the player and the AIs positions compared to what waypoints they are at, unfortunately they do not share the same waypoints
		//Lack of time means that this wont be implemented
	}
	
	public void LapFinishedByPlayer()
	{
		laps[playerIndex - planes.Length]++;
	}
	
	public Transform GetRandomWaypointContainer()
	{
		return( waypointContainers[Random.Range(0,waypointContainers.Length )]);
	}
	
}
