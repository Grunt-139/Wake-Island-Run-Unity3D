using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkidEnabler : MonoBehaviour 
{
	public WheelCollider wheelCollider;
//	public List<GameObject> skidTrailRenderer;
//	private List<TrailRenderer> skidMarks;
	
	public GameObject skidTrailRenderer;
	private TrailRenderer skidMark;
	
	public float skidLife = 4f;
//	private GameObject currentRendererObject;
//	private TrailRenderer currentSkidMark;
//	private int curIndex;
//	private int prevIndex;
//	private bool findNewTrail = false;
	
	void Start () 
	{
		skidMark = skidTrailRenderer.GetComponent<TrailRenderer>();
		//this avoids a visual bug on first use, if the art team set the effect’s time to 0.
		skidMark.time = skidLife;
//		skidMarks = new List<TrailRenderer>();
//		for(int i = 0; i < skidTrailRenderer.Count; i++)
//		{
//			skidMarks.Add(skidTrailRenderer[i].GetComponent<TrailRenderer>());
//			skidMarks[i].time = skidLife;
//		}
//		currentSkidMark = skidMarks[0];
//		currentRendererObject = skidTrailRenderer[0];
//		curIndex = 0;
//		prevIndex = 0;
	}

	
	// Update is called once per frame
	void Update () 
	{
		if(wheelCollider.forwardFriction.stiffness < 0.1f && wheelCollider.isGrounded)
	    {
	      //if skidMark’s time variable is 0 than we have reset it previously and can now use it.
	      if(skidMark.time == 0)
	      {
	        skidMark.time = skidLife;
	        skidTrailRenderer.transform.parent = wheelCollider.transform;
	        skidTrailRenderer.transform.localPosition = wheelCollider.center + ((wheelCollider.radius-0.1f) * -wheelCollider.transform.up);
	      }
	      //if this skid mark’s parent is null than we have previously used it and need to reset it first.
	      if(skidTrailRenderer.transform.parent == null)
	      {
	        skidMark.time = 0;
	      }
	    }
	    //unhook the skid effect game object from the wheel collider so it gets left behind.
	    else
	    {
	       skidTrailRenderer.transform.parent = null;
	    }	
		
//		print(curIndex + " " + prevIndex);
//		if(findNewTrail)
//		{
//			for(int i = 0; i < skidTrailRenderer.Count; i++)
//			{
//				if(i != prevIndex)
//				{
//					currentRendererObject = skidTrailRenderer[i];
//					currentSkidMark = skidMarks[i];
//					curIndex = i;
//					findNewTrail = false;
//					break;
//				}
//		  	}
//		}
//		
//		if(wheelCollider.forwardFriction.stiffness < 0.1f && wheelCollider.isGrounded)
//	    {	
//	      //if skidMark’s time variable is 0 than we have reset it previously and can now use it.
//	      if(currentSkidMark.time == 0)
//	      {
//	       	currentSkidMark.time = skidLife;
//	      	currentRendererObject.transform.parent = wheelCollider.transform;
//	        currentRendererObject.transform.localPosition = wheelCollider.center + ((wheelCollider.radius-0.1f) * -wheelCollider.transform.up);
//	      }
//	      //if this skid mark’s parent is null than we have previously used it and need to reset it first.
//	      if(currentRendererObject.transform.parent == null)
//	      {
//	      		currentSkidMark.time = 0;
//	      }
//	    }
//	    //unhook the skid effect game object from the wheel collider so it gets left behind.
//	    else
//	    {
//	       	currentRendererObject.transform.parent = null;
//			findNewTrail = true;
//			prevIndex = curIndex;
//	    }
	}
}
