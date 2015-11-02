using UnityEngine;
using System.Collections;

[RequireComponent(typeof(WheelCollider))]
public class SkidSoundEmitter : MonoBehaviour 
{
	public float skidAt = 1.5f;
	public int soundEmissionPerSound = 10;
	public AudioClip skidSound;
	
	private float soundDelay;
	private WheelCollider attachedWheel;
	
	// Use this for initialization
	void Start () 
	{
		//Always grab the component only once via start, dont do this in Update()
		attachedWheel = transform.GetComponent<WheelCollider>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		WheelHit hit;
		if(attachedWheel.GetGroundHit(out hit))
		{
			float frictionValue = Mathf.Abs(hit.sidewaysSlip);
			
			if(skidAt <= frictionValue && soundDelay <=0)
			{
				AudioSource.PlayClipAtPoint(skidSound,hit.point);
				soundDelay = 1f;
			}
		}
		soundDelay -=Time.deltaTime * soundEmissionPerSound;
	}
}
