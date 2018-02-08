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

public class GeneratePrisoners : MonoBehaviour {
    public GameObject prisoner;
    public int        prisonersRemaining;               // Number of prisoners in a prison
    private float     emergenceFrequency = 2.0f;        // Seconds between successive emergence of prisoners
    private float     timeSinceLastEmergence = 0.0f;    // Seconds since last prisoner emerged

	void Update () {
		if (prisonersRemaining > 0) {
            if (timeSinceLastEmergence >= emergenceFrequency) {
                timeSinceLastEmergence = 0.0f;
                GameObject.Instantiate(prisoner, transform.position + new Vector3(0, 0, -8),
                    Quaternion.Euler(0, 180, 0));
                prisonersRemaining--;
            }

            else timeSinceLastEmergence += Time.deltaTime;
        }
	}
}
