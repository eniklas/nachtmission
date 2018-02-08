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

public class ManageTank : MonoBehaviour {
    private GameObject chopper;
    private const int MAX_DISTANCE = 1000;  // Max distance to follow chopper
    private const int MIN_DISTANCE = 5;     // Stop following at this distance
    private const float SPEED = 4.0f;       // Speed of tank
    private float distanceFromChopper;      // Distance between tank and chopper
    private float enemyTerritoryBoundary;

	void Start () {
        chopper = GameObject.Find("Chopper");
        enemyTerritoryBoundary = GameObject.Find("LeftRiverBoundary").transform.position.x - 20;
	}
	
	void Update () {
		distanceFromChopper = transform.position.x - chopper.transform.position.x;

        if ((Mathf.Abs(distanceFromChopper) < MAX_DISTANCE && Mathf.Abs(distanceFromChopper) > MIN_DISTANCE)) {

            if (distanceFromChopper < 0 && transform.position.x < enemyTerritoryBoundary)
                transform.Translate(Vector3.right * Time.deltaTime * SPEED);

            else if (distanceFromChopper > 0) transform.Translate(Vector3.left * Time.deltaTime * SPEED);
        }
	}

    void OnCollisionStay(Collision col) {
        // Prevent tanks from running into each other
        if (col.gameObject.tag == "tank") {
            if (transform.position.x > col.gameObject.transform.position.x) 
                transform.Translate(Vector3.right * Time.deltaTime * SPEED);
            else transform.Translate(Vector3.left * Time.deltaTime * SPEED);
        }
    }
}
