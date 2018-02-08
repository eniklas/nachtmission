/* nachtmission
 * Copyright (c) 2017-2018 Erik Niklas
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowChopper : MonoBehaviour {
    private GameObject chopper;
    private float currentHeight;    // Current Y position of the chopper
    private float chopperMinHeight; // Minimun height the chopper can go
    private float chopperMaxHeight; // Maximun height the chopper can go
    private float chopperMidpoint;  // The vertical midpoint of the min and max height the chopper can go
    private float leeway = 0.5f;    // How closely the camera follows the chopper vertically, between 0.0 and 1.0
                                    //  0.0 means always looking directly at chopper; 1.0 means static at midpoint
    private float cameraMaxHeight;          // Camera will never go higher than this
    private float cameraZPos = -40.0f;      // The default Z position of the camera
    private float pullbackFactor = 0.0f;    // When chopper is this percentage near the max height, pull back
    private float pullbackThresholdMax;     // If the chopper goes above this height, pull back
    private float pullbackMultiplier = 2.0f;

	void Start () {
        chopper = GameObject.Find("Chopper");
        chopperMinHeight = GameObject.Find("Terrain").gameObject.transform.position.y +
            chopper.GetComponent<ControlChopper>().minHeightAboveGround;
        chopperMaxHeight = GameObject.Find("Ceiling").gameObject.transform.position.y;
        cameraMaxHeight = GameObject.Find("CameraCeiling").gameObject.transform.position.y;
        chopperMidpoint = (chopperMaxHeight - chopperMinHeight) / 2;

        pullbackThresholdMax = chopperMaxHeight - (chopperMaxHeight - chopperMinHeight) * pullbackFactor;
	}
	
    // Keep camera pointed at chopper
	void Update () {
        transform.position = new Vector3(chopper.transform.position.x, GetYPos(), GetZPos());
//		transform.position = new Vector3(chopper.transform.position.x, chopper.transform.position.y + 1, -20);
	}

    // Returns the Y position of the camera
    float GetYPos() {
        currentHeight = chopper.transform.position.y;

        if (currentHeight >= chopperMidpoint) return Mathf.Min(cameraMaxHeight, currentHeight - leeway * (currentHeight - chopperMidpoint));
        else return Mathf.Min(cameraMaxHeight, currentHeight + leeway * (chopperMidpoint - currentHeight));
    }

    // FIXME: pullback should accelerate as you get further towards the top; needs to be smoother
    float GetZPos() {
        if (chopper.transform.position.y > pullbackThresholdMax)
            return cameraZPos - (pullbackMultiplier * (chopper.transform.position.y - pullbackThresholdMax));

        else return cameraZPos;
    }
}
