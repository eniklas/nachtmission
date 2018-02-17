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

public class ManageJet : MonoBehaviour {
    private float       enemyTerritoryBoundary;         // X position dividing enemy and friendly territory
    private const float DEFAULT_SPEED = 25.0f;          // Default speed of jet
    private float       speed;                          // Current speed of jet
    private float       missileSpeed = 40.0f;           // Speed of missiles when fired
    private const int   ENGINE_SOUND_OFFSET = 0;        // Offset (in Inspector) for jet engine sound
    private const int   FIRE_SOUND_OFFSET = 1;          // Offset (in Inspector) for missile firing sound
    private float       finalRollAngle;                 // Z rotation at end of swoop
    private float       rollTime = 0.0f;                // How long jet has been rolling
    private float       roll;                           // Current roll angle
    private const float TIME_TO_SWOOP = 1.2f;           // How long it takes to swoop
    private float       finalSwoopAngle;                // Final angle about Y after swooping
    private const float SWOOP_DROP_DISTANCE = 5.0f;     // Drop this far while swooping
    private const float SWOOP_CHOPPER_DISTANCE = 20.0f; // X distance past chopper to swoop
    private float       swoopTime = 0.0f;               // How long jet has been swooping
    private float       swoopRotation = 0.0f;           // Current rotation about Y
    private float       initialYPos;                    // Initial Y position
    private float       initialZPos;                    // Initial Z position
    private float       initialRoll;                    // Initial roll angle
    private float       initialPitch;                   // Initial pitch angle
    private float       finalYPos;                      // Final Y position after swooping
    private float       finalZPos;                      // Final Z position after swooping
    private const float ASCENT_SPEED_FACTOR = 25.0f;    // Speed jet ascends when retreating
    private const float RETREAT_TIME_TO_ASCEND = 0.5f;  // How long before ascent once retreat begins
    private const float RETREAT_SPEED_FACTOR = 1.2f;    // Factor speed changes when retreating
    private const float RETREAT_PITCH_FACTOR = 0.02f;   // Pitch increase/sec when retreating
    private float       acceleration;                   // Current acceleration as jet speeds up to match chopper
    private float       accelerationTime = 0.0f;        // Time jet has been accelerating to match chopper
    private const float TIME_TO_ACCELERATE = 0.5f;      // How long it takes to match chopper's speed
    private const float MAX_ACCELERATION = 1.2f;        // Max acceleration after TIME_TO_ACCELERATE has passed
    private float       retreatTime = 0.0f;             // Time jet has been retreating
    private bool        isHunting = true;
    private bool        isSwooping = false;
    private bool        isRetreating = false;
    private bool        isHeadingRight;                 // True if jet is heading to the right; false if heading left
    private float       chopperStillMaxSpeed;           // Max speed at which chopper is considered to be still
    private bool        chopperMoving = false;          // True if chopper is nearly still; used for swooping
    private GameObject  chopper;
    private GameObject[] missiles = new GameObject[2];  // Array of missiles attached to jet
    public  GameObject  explosion;
    private AudioSource engineSound;
    private ParticleSystem.MainModule psMainLeftExhaust;  // Used to intensify jet exhaust during retreat
    private ParticleSystem.MainModule psMainRightExhaust;

    void Awake() {
        // Note that using GameObject.Find would always find the first
        //  missileLeft object, not necessarily the child of this jet
        missiles[0] = transform.Find("MissileLeft").gameObject;
        missiles[1] = transform.Find("MissileRight").gameObject;

        foreach (GameObject missile in missiles) {
            missile.GetComponent<Rigidbody>().detectCollisions = false;             // Disable Rigidbody until launched
            missile.transform.Find("MissileExhaust").gameObject.SetActive(false);   // Disable exhaust effect
        }

        psMainLeftExhaust = transform.Find("JetExhaustLeft").GetComponent<ParticleSystem>().main;
        psMainRightExhaust = transform.Find("JetExhaustRight").GetComponent<ParticleSystem>().main;
        engineSound = GetComponents<AudioSource>()[ENGINE_SOUND_OFFSET];
    }

	void Start () {
		chopper = GameObject.Find("Chopper");
        enemyTerritoryBoundary = GameObject.Find("LeftRiverBoundary").transform.position.x;
        chopperStillMaxSpeed = GameObject.Find("Enemies").GetComponent<SpawnEnemies>().chopperStillMaxSpeed;

        // Orient the jet depending on whether it was spawned to the left or right of chopper
        if (transform.position.x > chopper.transform.position.x) {
            isHeadingRight = false;
            transform.Rotate(0, 270, 0);    // Jet facing left, wings parallel to Z axis
            finalSwoopAngle = 90.0f;
            finalRollAngle = -90.0f;
            speed = -DEFAULT_SPEED;
        }
        else {
            isHeadingRight = true;
            transform.Rotate(0, 90, 0);     // Jet facing right
            finalSwoopAngle = 270.0f;
            finalRollAngle = 90.0f;
            speed = DEFAULT_SPEED;
        }

        initialYPos = transform.position.y;
        initialZPos = transform.position.z;
        initialRoll = transform.eulerAngles.z;
        initialPitch = transform.eulerAngles.y;
        finalZPos = chopper.transform.position.z;
	}
	
	void FixedUpdate () {
        if (isRetreating)
            foreach (GameObject missile in missiles)
                if (missile != null)
                    missile.GetComponent<Rigidbody>().AddForce(Vector3.down * 20, ForceMode.Acceleration);
    }

	void Update () {
        if (Time.frameCount % 25 == 0) Debug.Log("Jet speed = " + speed);
        transform.position = new Vector3(GetXPos(), GetYPos(), GetZPos());
        // Could also use a finite state machine for this
        if (isHunting) Hunt();
        else if (isSwooping) Swoop();
        else if (isRetreating) Retreat();
	}

    private float GetXPos() {
        if (isSwooping) {
            accelerationTime += Time.deltaTime;

            if (chopperMoving) {
                if (accelerationTime < TIME_TO_ACCELERATE)
                    acceleration = MAX_ACCELERATION * accelerationTime / TIME_TO_ACCELERATE;
                else
                    acceleration = MAX_ACCELERATION;
            }
            else acceleration = 0;

            if (swoopTime < TIME_TO_SWOOP / 2)
                return transform.position.x + ((TIME_TO_SWOOP / 2) - swoopTime) * Time.deltaTime * speed +
                    (acceleration * speed * Time.deltaTime);

            else
                return transform.position.x - (swoopTime - (TIME_TO_SWOOP / 2)) * Time.deltaTime * speed +
                    (acceleration * speed * Time.deltaTime);
        }

        else return transform.position.x + Time.deltaTime * speed;
    }

    private float GetYPos() {
        if (isSwooping) return transform.position.y - Time.deltaTime / TIME_TO_SWOOP * (initialYPos - finalYPos);

        else if (isRetreating && retreatTime >= RETREAT_TIME_TO_ASCEND)
            // Use retreat time to accelerate lift
            return transform.position.y + Time.deltaTime * ASCENT_SPEED_FACTOR * (retreatTime - RETREAT_TIME_TO_ASCEND);

        else return transform.position.y;
    }

    private float GetZPos() {
        if (isSwooping) return initialZPos - (swoopTime / TIME_TO_SWOOP) * (initialZPos - finalZPos);
        else return transform.position.z;
    }

    void Hunt() {
        // Fly a bit further past the chopper if it's relatively still, so the chopper will get
        //  shot down if it doesn't react to the swooping jet, whether it's moving or not
		if (isHeadingRight &&
           ((transform.position.x > chopper.transform.position.x + (SWOOP_CHOPPER_DISTANCE / 2) &&
            chopperMoving) ||
		   (transform.position.x > chopper.transform.position.x + SWOOP_CHOPPER_DISTANCE &&
            !chopperMoving)) ||

		   (!isHeadingRight &&
           ((transform.position.x < chopper.transform.position.x - (SWOOP_CHOPPER_DISTANCE / 2) &&
            chopperMoving) ||
		   (transform.position.x < chopper.transform.position.x - SWOOP_CHOPPER_DISTANCE &&
            !chopperMoving)))) {
                isHeadingRight = !isHeadingRight;   // Change direction

                Debug.Log("Chopper hspeed = " + chopper.GetComponent<ControlChopper>().hSpeed);
                if (Mathf.Abs(chopper.GetComponent<ControlChopper>().hSpeed) > chopperStillMaxSpeed) {
                    chopperMoving = true;
                    Debug.Log("chopperMoving = true");
                }
                Swoop();
        }

        // If we end up right of the river, take off eh (should only happen while hunting)
        if (transform.position.x > enemyTerritoryBoundary) Retreat();
    }

    void Roll() {
        rollTime += Time.deltaTime;

        if (rollTime >= TIME_TO_SWOOP)
            if (isHeadingRight)
                transform.Rotate(Vector3.forward, 1.5f, Space.Self);
            else
                // TODO: understand why -1.5f works
                transform.Rotate(Vector3.forward, -1.5f, Space.Self);
        else {
            roll = Time.deltaTime / TIME_TO_SWOOP * (initialRoll - finalRollAngle);
            transform.Rotate(Vector3.forward, roll, Space.Self);
        }
    }

    void Swoop() {
        if (!isSwooping) {    // First call
            isHunting = false;
            isRetreating = false;
            isSwooping = true;
            // Match chopper speed if it's moving
            if (chopperMoving) {
                speed = 1.5f * chopper.GetComponent<ControlChopper>().hSpeed;
                Debug.Log("Starting swoop. Matching chopper speed at " + speed);
            }
            else Debug.Log("Starting swoop. Chopper is still at " + chopper.GetComponent<ControlChopper>().hSpeed);

            // Box collider across wings is long while hunting so it passes through Z=0, but
            //  once it swoops we need to change the collider to match the actual wingspan
            transform.GetComponent<BoxCollider>().size = new Vector3(7,
                transform.GetComponent<BoxCollider>().size.y,
                transform.GetComponent<BoxCollider>().size.z);
        }

        // If chopper is sufficiently below jet when it swoops, aim for SWOOP_DROP_DISTANCE units
        //  above chopper; otherwise, just drop SWOOP_DROP_DISTANCE units from initial height
        if (transform.position.y >= chopper.transform.position.y + SWOOP_DROP_DISTANCE)
            finalYPos = transform.position.y - SWOOP_DROP_DISTANCE;
        else finalYPos = initialYPos - SWOOP_DROP_DISTANCE;

        swoopTime += Time.deltaTime;
        swoopRotation = Time.deltaTime * (finalSwoopAngle - initialPitch) / TIME_TO_SWOOP;

        if (swoopTime >= TIME_TO_SWOOP) {
            // FIXME: this doesn't work; need to fiddle with finalSwoopAngle and possibly swoopRotation formula
//              transform.Rotate(Vector3.up, finalSwoopAngle, Space.World);
            Attack();
        }
        else transform.Rotate(Vector3.up, swoopRotation, Space.World);

        Roll();
    }

    void Attack() {
        // Fire; detach missiles from jet, enable colliders and particle effects, and add force
        foreach (GameObject missile in missiles) {
            missile.transform.parent = null;
            missile.GetComponent<Rigidbody>().detectCollisions = true;
            missile.transform.Find("MissileExhaust").gameObject.SetActive(true);   // Enable exhaust effect

            // Add horizontal force; downward acceleration is done in FixedUpdate()
            if (isHeadingRight)
                missile.GetComponent<Rigidbody>().AddForce(Vector3.right * missileSpeed, ForceMode.VelocityChange);
            else
                missile.GetComponent<Rigidbody>().AddForce(Vector3.left * missileSpeed, ForceMode.VelocityChange);
        }

        GetComponents<AudioSource>()[FIRE_SOUND_OFFSET].Play();
        Retreat();
    }

    // TODO: it looks cool when they fly off into +Z; do that sometimes
    void Retreat() {
        if (!isRetreating) {
Debug.Log("Starting retreat.");
            isHunting = false;
            isRetreating = true;
            isSwooping = false;
            psMainLeftExhaust.startSizeMultiplier = 5;      // Intensify jets
            psMainRightExhaust.startSizeMultiplier = 5;
            if (!chopperMoving) {
                if (isHeadingRight) speed = DEFAULT_SPEED;
                else speed = -DEFAULT_SPEED;
                Debug.Log("Chopper not moving. Initial retreat speed = " + speed);
            }
        }

        retreatTime += Time.deltaTime;
        if (isHeadingRight)
            speed += retreatTime * RETREAT_SPEED_FACTOR;
        else
            speed -= retreatTime * RETREAT_SPEED_FACTOR;

        // Increase pitch of engine as we retreat. If we were using forces to move jet, Doppler Level would do this
        engineSound.pitch += retreatTime * RETREAT_PITCH_FACTOR;

        // Note that this will be true if the Scene camera can see it, so the jet may stay around longer in dev
        if (Mathf.Abs(transform.position.x - chopper.transform.position.x) > 100 && !GetComponent<Renderer>().isVisible)
            // Don't destroy the jet too early, otherwise the missiles may
            //  slow down before impact because the script has exited
            Destroy(gameObject, 3);
    }

    void OnCollisionEnter(Collision col) {
        // Wide collider on jet makes it too easy to collide; don't collide before attack
        if (col.gameObject.tag == "chopper" && isRetreating) {
            GameObject expClone = GameObject.Instantiate(explosion, col.gameObject.transform.position,
                Quaternion.identity);
            Destroy(expClone, 3);   // Explosion lasts 3 secs
            Destroy(gameObject);
            col.gameObject.GetComponent<ControlChopper>().Crash();
        }
    }
}
