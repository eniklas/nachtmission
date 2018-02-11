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

public class ControlTurret : MonoBehaviour {
    private GameObject chopper;
    private GameObject turretMuzzle;
    private GameObject turretMuzzleTail;
    private ParticleSystem muzzleFlashPS;
    private float rotationSpeed = 20.0f;                // Turret rotation speed
    private const float MIN_ROTATION = 315.0f;          // Leftmost rotation (-45)
    private const float MAX_ROTATION = 45.0f;           // Rightmost rotation
    private const float MAX_DISTANCE_TO_FIRE = 20.0f;   // Fire at chopper if it's closer than this on X axis
    private const float RELOAD_TIME = 2.0f;             // How often tank can fire
    private float timeSinceLastFire = 0.0f;             // Time since tank last fired
    private bool canFire = true;                        // True if the tank is ready to fire
    private const float bulletSpeed = 3.0f;
    public GameObject bullet;
    private GameObject bulletClone;

	void Awake () {
        // Bullet is instantiated on turretMuzzle; turretMuzzleTail is 1 unit away and the difference between
        //  the two gives a unit vector in the direction the turret is aiming. Both objects are zero size.
		turretMuzzle = transform.Find("TankMuzzle").gameObject;
		turretMuzzleTail = transform.Find("TankMuzzleTail").gameObject;
        muzzleFlashPS = transform.Find("TankMuzzleFlash").gameObject.GetComponent<ParticleSystem>();
    }

	void Start () {
		chopper = GameObject.Find("Chopper");
	}
	
	void Update () {
        // Chopper is to the left of tank
        if (chopper.transform.position.x < transform.position.x) {
            // Left of center decreases from 360 degrees
            if (transform.eulerAngles.y > 180.0f && transform.eulerAngles.y <= MIN_ROTATION)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, MIN_ROTATION, transform.eulerAngles.z);

            else transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
        }

        // Chopper is to the right of tank
        else if (chopper.transform.position.x > transform.position.x) {
            if (transform.eulerAngles.y < 180.0f && transform.eulerAngles.y >= MAX_ROTATION)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, MAX_ROTATION, transform.eulerAngles.z);

            else transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        if (canFire) {
            if (Mathf.Abs(chopper.transform.position.x - transform.position.x) < MAX_DISTANCE_TO_FIRE) {
                Fire();
                canFire = false;
                timeSinceLastFire = 0.0f;
            }
        }

        else {
            timeSinceLastFire += Time.deltaTime;
            if (timeSinceLastFire >= RELOAD_TIME) canFire = true;
        }
	}

    void Fire() {
        muzzleFlashPS.Play();
        GetComponent<AudioSource>().Play();
        bulletClone = GameObject.Instantiate(bullet, turretMuzzle.transform.position, Quaternion.identity);

        // Since we have a wide collider for the tank, we need to track who shot
        //  each bullet to prevent the tank from shooting itself
        bulletClone.tag = "source:tank";
        bulletClone.GetComponent<Rigidbody>().useGravity = true;
        bulletClone.GetComponent<Rigidbody>().AddForce((turretMuzzle.transform.position -
            turretMuzzleTail.transform.position) * bulletSpeed, ForceMode.VelocityChange);
    }
}
