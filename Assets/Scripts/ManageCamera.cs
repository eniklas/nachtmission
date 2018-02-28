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

public class ManageCamera : MonoBehaviour {
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

    private GameObject  title;                         // 3D model of title
    private const float TITLE_ZOOM_WAIT_TIME = 4.0f;   // How long to wait after game launch before zooming title
    private const float TITLE_Z_OFFSET_UNZOOMED = 20;  // Z offset from camera of title when unzoomed (showing)
    private const float TITLE_Z_OFFSET_ZOOMED = -5;    // Z offset from camera of title when zoomed (hidden)
    public  float       titleZoomDuration = 1.0f;      // How long it takes for title to zoom in/out
    public  float       titleZoomTime;                 // How long the title has been zooming
    public  bool        titleZoomIn;                   // True if title should zoom in, false if it should zoom out
    public  bool        titleIsZooming = false;        // True if title is zoomed, false if unzoomed

    void Awake() {
        chopper = GameObject.Find("Chopper");
        chopperMinHeight = GameObject.Find("Terrain").gameObject.transform.position.y +
            chopper.GetComponent<ManageChopper>().minHeightAboveGround;
        // Chopper can go slightly above ceiling since we're not correcting the height if it does
        chopperMaxHeight = GameObject.Find("Ceiling").gameObject.transform.position.y + 2;
        cameraMaxHeight = GameObject.Find("CameraCeiling").gameObject.transform.position.y;
        chopperMidpoint = (chopperMaxHeight - chopperMinHeight) / 2;

        pullbackThresholdMax = chopperMaxHeight - (chopperMaxHeight - chopperMinHeight) * pullbackFactor;
        transform.position = new Vector3(chopper.transform.position.x, GetYPos(), GetZPos());

        // Put title just above the menu
        title = transform.Find("Title").gameObject;
        title.transform.position = new Vector3(transform.position.x,
                                               transform.position.y + (chopperMaxHeight / 6),
                                               transform.position.z + TITLE_Z_OFFSET_ZOOMED);
    }

	void Update () {
        if (titleIsZooming)
            ZoomTitle();
	}

	void LateUpdate () {
        // Keep camera pointed at chopper
        transform.position = new Vector3(chopper.transform.position.x, GetYPos(), GetZPos());
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

    // Zooms the title in or out
    public void ZoomTitle() {
        // First call
        if (!titleIsZooming) {
            titleIsZooming = true;
            titleZoomTime = 0.0f;
            if (!title.activeSelf)
                title.SetActive(true);
        }

        // The Unity splash screen seems to cause the zoom to not work
        //  if done immediately at launch, so wait a few seconds
        if (Time.unscaledTime < TITLE_ZOOM_WAIT_TIME) return;

        // Game may be paused, so use unscaled time
        titleZoomTime += Time.unscaledDeltaTime;

        if (titleZoomTime < titleZoomDuration) {
            if (titleZoomIn) {
                title.transform.Translate(Vector3.forward * (TITLE_Z_OFFSET_UNZOOMED - TITLE_Z_OFFSET_ZOOMED)
                    * (Time.unscaledDeltaTime / titleZoomDuration));
            }

            else {
                title.transform.Translate(Vector3.back * (TITLE_Z_OFFSET_UNZOOMED - TITLE_Z_OFFSET_ZOOMED)
                    * (Time.unscaledDeltaTime / titleZoomDuration));
            }
        }
        else {
            titleIsZooming = false;

            // Once title has zoomed out of view, disable it and the spotlight
            if (titleZoomIn) {
                title.SetActive(false);
                GameObject.Find("Spotlight").SetActive(false);
            }
        }
    }
}
