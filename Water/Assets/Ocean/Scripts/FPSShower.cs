using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSShower : MonoBehaviour
{

    private Text fpsText;
    [SerializeField] private Text wsText;
	[SerializeField] private bool showFPS;
	[SerializeField] private bool showWaveStateName;

    private float fps;

    private static string wsName;

    public static void SetWaveStateString(string name)
    {
	    wsName = name;
    }
    // Start is called before the first frame update
    void Start()
    {
	    fpsText = GetComponent<Text>();
	    
	    
    }

    // Update is called once per frame
    void Update()
    {
	    if (Input.GetKeyDown(KeyCode.O))
		    showFPS = !showFPS;
	    if (Input.GetKeyDown(KeyCode.P))
		    showWaveStateName = !showWaveStateName;

	    if (showFPS)
		{
			fps = Time.frameCount / Time.time;
			fpsText.text = fps.ToString();
		}
		else
	    {
		    fpsText.text = "";
	    }

		if (showWaveStateName)
		{
			wsText.text = wsName;
		}
		else
		{
			wsText.text = "";
		}
		
		


    }
}
