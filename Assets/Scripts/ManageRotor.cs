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

public class ManageRotor : MonoBehaviour {
    private ManageChopper chopperScript;
    private float minSpeed;                         // Current minimum speed of rotor
    private const float MIN_SPEED_CRASHING = 0.0f;  // Degrees/sec at min rotor speed when crashing
    private const float MIN_SPEED = 500.0f;         // Degrees/sec at min rotor speed
    private const float MAX_SPEED = 1250.0f;        // Degrees/sec at max rotor speed
    private const float TIME_TO_SPIN_UP = 3.0f;     // Time it takes rotor to spin up from MIN_SPEED
    private const float TIME_TO_SPIN_DOWN = 6.0f;   // Time it takes rotor to spin down from MAX_SPEED
    private float speed;                            // Current speed of rotor
    private float rotorSpinTime;                    // Time rotor has been spinning up/down
    private bool  spinningUp = false;               // Whether rotor is currently spinning up
    private bool  spinningDown = false;             // Whether rotor is currently spinning down
    private bool  isCrashing = false;               // Whether chopper is crashing
    private bool  isTailRotor = false;              // Whether this is the main rotor or tail rotor
    private AudioSource rotorSound;
    private const float MAX_SOUND_PITCH = 0.75f;    // Pitch of rotor sound when rotor is fully spun up

    void Awake() {
        speed = MIN_SPEED;
        minSpeed = MIN_SPEED;
        isCrashing = false;
        spinningUp = false;
        spinningDown = false;

        if (gameObject.tag == "tailRotor") isTailRotor = true;

        // Rotor sound only applies to main rotor
        else {
            rotorSound = GetComponent<AudioSource>();
            rotorSound.pitch = MAX_SOUND_PITCH * (speed / MAX_SPEED);
        }
    }

    void Start() {
        chopperScript = GameObject.Find("Chopper").GetComponent<ManageChopper>();
    }

	void Update () {
        if (speed < MAX_SPEED && !spinningUp && !chopperScript.isOnGround) {
            // Check initial speed when we start to spin up
            rotorSpinTime = TIME_TO_SPIN_UP * (speed - minSpeed) / (MAX_SPEED - minSpeed);

            spinningUp = true;
            spinningDown = false;
        }

        else if (speed > minSpeed && !spinningDown && chopperScript.isOnGround) {
            // Check initial speed when we start to spin down
            rotorSpinTime = TIME_TO_SPIN_DOWN * (MAX_SPEED - speed) / (MAX_SPEED - minSpeed);

            spinningDown = true;
            spinningUp = false;
        }

        // Crashing; spin down rotor to zero
        if (chopperScript.isCrashing && !isCrashing) {
            isCrashing = true;
            minSpeed = MIN_SPEED_CRASHING;
            spinningUp = false;
            spinningDown = true;
        }

        // Crash is done; reset
        else if (!chopperScript.isCrashing && isCrashing) Awake();

        // Change pitch of rotor sound when changing speed
        if (!isTailRotor && (spinningUp || spinningDown)) rotorSound.pitch = MAX_SOUND_PITCH * (speed / MAX_SPEED);

        if (spinningUp) Spinup();
        else if (spinningDown) Spindown();

        if (isTailRotor) { transform.Rotate(-Vector3.forward, speed * Time.deltaTime); }
        else { transform.Rotate(Vector3.up, speed * Time.deltaTime); }
	}

    // Spins up the rotor blade
    private void Spinup() {
        if (rotorSpinTime < TIME_TO_SPIN_UP) {
            rotorSpinTime += Time.deltaTime;
            speed = minSpeed + rotorSpinTime / TIME_TO_SPIN_UP * MAX_SPEED;
            
            if (speed > MAX_SPEED) {
                speed = MAX_SPEED;

                if (!isTailRotor) rotorSound.pitch = MAX_SOUND_PITCH;
            }
        }
        else spinningUp = false;
    }

    // Spins down the rotor blade
    private void Spindown() {
        if (rotorSpinTime < TIME_TO_SPIN_DOWN) {
            rotorSpinTime += Time.deltaTime;

            speed = MAX_SPEED - (rotorSpinTime / TIME_TO_SPIN_DOWN * MAX_SPEED);
            if (speed < minSpeed) speed = minSpeed;
        }
        else spinningDown = false;
    }
}
