using System;
using System.Collections;
using System.Collections.Generic;
using GibsOcean;
using UnityEngine;

public class TestingThing : MonoBehaviour
{
    [SerializeField] private WaveGen waveGen;
    private void FixedUpdate()
    {
        float waveHeight = waveGen.GetWaterHeight(transform.position);
        transform.position = new Vector3(transform.position.x, waveHeight, transform.position.z);
    }
}
