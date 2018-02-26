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

public class ManageDrone : MonoBehaviour {
    private const float Z_POS = 0.0f;
    private const float SPEED_X = 5.0f;
    private const float SPEED_Y = 2.5f;
    private const float BELT_SPEED = 2.5f;          // Speed the center belt rotates
    private const float BULLET_SPEED_X = 5.0f;
    private const float DAMPER_ZONE = 0.8f;
    private const float DAMPER_SPEED = 0.5f;
    private const float MAX_CHOPPER_X_OFFSET = 1.0f;
    private const float MAX_CHOPPER_Y_OFFSET = 1.0f;
    private const float FIRING_FREQUENCY = 1.0f;    // How often drone fires
    private const float FIRING_RANGE = 30.0f;       // X distance from chopper to start firing
    private const int SOUND_FIRE = 1;               // Offset (in Inspector) for firing sound (local to GameObject)
    private const int SOUND_CRASH = 6;              // Offset for crash sound in Sounds prefab
    private float timeSinceFire = 0.0f;             // Time since drone last fired
    private float distanceX;                        // X distance between chopper and drone
    private float distanceY;                        // Y distance between chopper and drone
    private bool isUsingLeftMuzzle = true;          // True to fire left, false to fire right
    private GameObject leftMuzzle;
    private GameObject rightMuzzle;
    private ParticleSystem leftMuzzleFlashPS;
    private ParticleSystem rightMuzzleFlashPS;
    private GameObject belt;
    private GameObject chopper;
    public GameObject bullet;
    public GameObject bulletClone;
    public GameObject explosion;
    public GameObject sounds;

	void Awake() {
		leftMuzzle = transform.Find("LeftMuzzle").gameObject;
		rightMuzzle = transform.Find("RightMuzzle").gameObject;
        leftMuzzleFlashPS = transform.Find("LeftMuzzleFlash").gameObject.GetComponent<ParticleSystem>();
        rightMuzzleFlashPS = transform.Find("RightMuzzleFlash").gameObject.GetComponent<ParticleSystem>();
		belt = transform.Find("Belt").gameObject;
	}

    void Start() {
        chopper = GameObject.Find("Chopper");
    }
	
	void Update () {
        transform.position = new Vector3(GetXPos(), GetYPos(), Z_POS);
		timeSinceFire += Time.deltaTime;

        if (timeSinceFire >= FIRING_FREQUENCY &&
            (Mathf.Abs(transform.position.x - chopper.transform.position.x) <= FIRING_RANGE)) {
                Fire();
                isUsingLeftMuzzle = !isUsingLeftMuzzle; // Alternate firing side
                timeSinceFire = 0;
        }

        belt.transform.Rotate(Vector3.up, BELT_SPEED, Space.World);
	}

    private float GetXPos() {
        distanceX = transform.position.x - chopper.transform.position.x;

        if (distanceX > MAX_CHOPPER_X_OFFSET)
            return transform.position.x - SPEED_X * Time.deltaTime;

        else if (distanceX < -MAX_CHOPPER_X_OFFSET)
            return transform.position.x + SPEED_X * Time.deltaTime;

        else if (distanceX >= MAX_CHOPPER_X_OFFSET * DAMPER_ZONE)
            return transform.position.x - SPEED_X * DAMPER_SPEED * Time.deltaTime;

        else if (distanceX <= -MAX_CHOPPER_X_OFFSET * DAMPER_ZONE)
            return transform.position.x + SPEED_X * DAMPER_SPEED * Time.deltaTime;

        else if (distanceX >= 0)
            return transform.position.x - SPEED_X * Time.deltaTime;

        else return transform.position.x + SPEED_X * Time.deltaTime;
    }

    private float GetYPos() {
        distanceY = transform.position.y - chopper.transform.position.y;

        if (distanceY > MAX_CHOPPER_Y_OFFSET)
            return transform.position.y - SPEED_Y * Time.deltaTime;

        else if (distanceY < -MAX_CHOPPER_Y_OFFSET)
            return transform.position.y + SPEED_Y * Time.deltaTime;

        else if (distanceY >= MAX_CHOPPER_Y_OFFSET * DAMPER_ZONE)
            return transform.position.y - SPEED_Y * DAMPER_SPEED * Time.deltaTime;

        else if (distanceY <= -MAX_CHOPPER_Y_OFFSET * DAMPER_ZONE)
            return transform.position.y + SPEED_Y * DAMPER_SPEED * Time.deltaTime;

        else if (distanceY >= 0)
            return transform.position.y - SPEED_Y * Time.deltaTime;

        else return transform.position.y + SPEED_Y * Time.deltaTime;
    }

    void Fire() {
            if (isUsingLeftMuzzle) {
                bulletClone = GameObject.Instantiate(bullet, leftMuzzle.transform.position, Quaternion.identity);
                bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.left * BULLET_SPEED_X,
                    ForceMode.VelocityChange);

                // Add a little extra force in the direction we're moving to keep both sides looking symmetrical
                if (chopper.transform.position.x < transform.position.x)
                    bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.left * 1.5f * BULLET_SPEED_X,
                        ForceMode.VelocityChange);

                leftMuzzleFlashPS.Play();
            }

            else {
                bulletClone = GameObject.Instantiate(bullet, rightMuzzle.transform.position, Quaternion.identity);
                bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.right * BULLET_SPEED_X,
                    ForceMode.VelocityChange);

                if (chopper.transform.position.x > transform.position.x)
                    bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.right * 1.5f * BULLET_SPEED_X,
                        ForceMode.VelocityChange);

                rightMuzzleFlashPS.Play();
            }

            GetComponents<AudioSource>()[SOUND_FIRE].Play();
            bulletClone.GetComponent<Rigidbody>().useGravity = true;
            // Don't shoot ourselves
            Physics.IgnoreCollision(bulletClone.GetComponent<Collider>(), GetComponent<Collider>(), true);
    }

    void PlaySound(int offset) {
        GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
        soundsClone.GetComponents<AudioSource>()[offset].Play();
        Destroy(soundsClone, soundsClone.GetComponents<AudioSource>()[offset].clip.length);
    }

    void OnCollisionEnter(Collision col) {
        if (col.gameObject.tag == "chopper") {
            PlaySound(SOUND_CRASH);
            GameObject expClone = GameObject.Instantiate(explosion, col.gameObject.transform.position,
                Quaternion.identity);
            Destroy(expClone, 3);   // Explosion lasts 3 secs
            col.gameObject.GetComponent<ManageChopper>().Crash();
            Destroy(gameObject);
        }

        else if (col.gameObject.tag == "jet" || col.gameObject.tag == "tank") {
            PlaySound(SOUND_CRASH);
            GameObject expClone = GameObject.Instantiate(explosion, col.gameObject.transform.position,
                Quaternion.identity);
            Destroy(expClone, 3);
            Destroy(gameObject);
            Destroy(col.gameObject);
        }
    }
}
