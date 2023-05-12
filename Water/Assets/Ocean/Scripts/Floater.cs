using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GibsOcean
{
	public class Floater : MonoBehaviour
	{
		// Start is called before the first frame update
		private WaveGen waveGen;
		public Rigidbody rb;
		public float depthBeforeSubmerged = 1f;
		public float displacementAmount = 3f;
		public int floaterCount;
		public float waterDrag = 0.99f;
		public float waterAngularDrag = 0.5f;
		void Start()
		{
			waveGen = GameObject.Find("Ocean").GetComponent<WaveGen>();
		}

		private void FixedUpdate()
		{
			rb.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);

			float waveHeight = waveGen.GetWaterHeight(transform.position);
			if (transform.position.y < waveHeight)
			{
				float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) *
											   displacementAmount;
				rb.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f), transform.position, ForceMode.Acceleration);
				rb.AddForce(displacementMultiplier * -rb.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
				rb.AddTorque(displacementMultiplier * -rb.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
			}
		}
	}
}