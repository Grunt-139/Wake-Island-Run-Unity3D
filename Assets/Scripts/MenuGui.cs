using UnityEngine;
using System.Collections;

public class MenuGui : MonoBehaviour 
{
	public Texture2D background;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
	
	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));	
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(background);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.EndArea();	
		
		GUILayout.BeginArea(new Rect(Screen.width *0.5f, Screen.height * 0.5f,Screen.width,Screen.height));
		if( GUILayout.Button("Play", GUILayout.Width(Screen.width  * 0.15f), GUILayout.Height(Screen.width  * 0.05f) ) )
		{
			Application.LoadLevel(Application.loadedLevel+1);
		}
		if(GUILayout.Button("Quit",GUILayout.Width(Screen.width  * 0.15f), GUILayout.Height(Screen.width  * 0.05f) ) )
		{
			Application.Quit();
		}
		GUILayout.EndArea();
	}
}
