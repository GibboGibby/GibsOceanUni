using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSShower : MonoBehaviour
{

    private Text text;
	[SerializeField] private bool showFPS;

    private float fps;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
		if (showFPS)
		{
			fps = Time.frameCount / Time.time;
			text.text = fps.ToString();
		}
		else
		{
			text.text = "";
		}			

        
    }
}
