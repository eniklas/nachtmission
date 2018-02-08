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

// TODO: jet should attack from right sometimes, e.g. when taking prisoners back
// TODO: jet should attack head on sometimes, roll without swoop
// TODO: missile trajectory needs improving; swoop should happen closer. Use drag or a retardant force over time
// TODO: jet should shoot chopper down if it doesn't stop (currently can just ignore it)
public class ManageJet : MonoBehaviour {
    private float       enemyTerritoryBoundary;         // X position dividing enemy and friendly territory
    private float       speed = 25.0f;                  // Horizontal speed
    private float       missileSpeed = 50.0f;
    private const int   ENGINE_SOUND_OFFSET = 0;        // Offset (in Inspector) for jet engine sound
    private const int   FIRE_SOUND_OFFSET = 1;          // Offset (in Inspector) for missile firing sound
    private const float TIME_TO_ACCELERATE = 0.5f;      // How long it takes to match chopper's speed
    private const float TIME_TO_ROLL = 1.0f;            // How long it takes to roll
    private const float FINAL_ROLL_ANGLE = 90.0f;       // Z rotation
    private float       rollTime = 0.0f;                // How long jet has been rolling
    private float       roll;                           // Current roll angle
    private const float TIME_TO_SWOOP = 1.0f;           // How long it takes to swoop
    private const float FINAL_SWOOP_ANGLE = 270.0f;     // Final angle about Y after swooping
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
    private float       accelerationTime = 0.0f;        // Time jet has been acceleration to match chopper
    private float       retreatTime = 0.0f;             // Time jet has been retreating
    private bool        isHunting = true;
    private bool        isSwooping = false;
    private bool        isRetreating = false;
    private bool        isHeadingRight = true;          // True if jet is heading to the right; false if heading left
    private float       CHOPPER_STILL_MAX_SPEED = 10.0f; // Max speed at which chopper is considered to be still
    private bool        chopperMoving = false;          // True if chopper is nearly still; used for swooping
    private GameObject  chopper;
    private GameObject  missileLeft;
    private GameObject  missileRight;
    public  GameObject  explosion;
    private AudioSource engineSound;
    private ParticleSystem.MainModule psMainLeftExhaust;  // Used to intensify jet exhaust during retreat
    private ParticleSystem.MainModule psMainRightExhaust;

    void Awake() {
        transform.Rotate(0, 90, 0);    // Orient the jet facing right, wings parallel to Z axis
        initialYPos = transform.position.y;
        initialZPos = transform.position.z;
        initialRoll = transform.eulerAngles.z;
        initialPitch = transform.eulerAngles.y;
        // Note that using GameObject.Find would always find the first
        //  missileLeft object, not necessarily the child of this jet
        missileLeft = transform.Find("MissileLeft").gameObject;
        missileLeft.GetComponent<Rigidbody>().detectCollisions = false;             // Disable Rigidbody until launched
        missileLeft.transform.Find("MissileExhaust").gameObject.SetActive(false);   // Disable exhaust effect
        missileRight = transform.Find("MissileRight").gameObject;
        missileRight.GetComponent<Rigidbody>().detectCollisions = false;
        missileRight.transform.Find("MissileExhaust").gameObject.SetActive(false);
        psMainLeftExhaust = transform.Find("JetExhaustLeft").GetComponent<ParticleSystem>().main;
        psMainRightExhaust = transform.Find("JetExhaustRight").GetComponent<ParticleSystem>().main;
        engineSound = GetComponents<AudioSource>()[ENGINE_SOUND_OFFSET];
    }

	void Start () {
		chopper = GameObject.Find("Chopper");
        finalZPos = chopper.transform.position.z;
        enemyTerritoryBoundary = GameObject.Find("LeftRiverBoundary").transform.position.x;
	}
	
	void Update () {
        if (Time.frameCount % 10 == 0) Debug.Log("Jet speed = " + speed);
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
                if (accelerationTime < TIME_TO_ACCELERATE) acceleration = accelerationTime / TIME_TO_ACCELERATE;
                else acceleration = 1.0f;
                Debug.Log("acceleration = " + acceleration);
            }
            else acceleration = 0;

            if (swoopTime < TIME_TO_SWOOP / 2)
                return transform.position.x + ((TIME_TO_SWOOP / 2) - swoopTime) * Time.deltaTime * speed + (acceleration * speed * Time.deltaTime);
            else
                return transform.position.x - (swoopTime - (TIME_TO_SWOOP / 2)) * Time.deltaTime * speed + (acceleration * speed * Time.deltaTime);
        }

        else if (isRetreating) return transform.position.x - Time.deltaTime * Mathf.Abs(speed);
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
        // Fly to the right until we've just passed the chopper; swoop closer if chopper is moving
        if (Mathf.Abs(transform.position.x - chopper.transform.position.x) < SWOOP_CHOPPER_DISTANCE &&
            Mathf.Abs(chopper.GetComponent<ControlChopper>().hSpeed) > CHOPPER_STILL_MAX_SPEED) {
                chopperMoving = true;
                Debug.Log("Chopper hspeed = " + chopper.GetComponent<ControlChopper>().hSpeed + ", chopperMoving = true");
        }

        // Fly a bit further past the chopper if it's relatively still, so the chopper will get shot down
        //  if it doesn't react to the swooping jet, independent of whether it's moving and how fast
		if ((transform.position.x > chopper.transform.position.x + (SWOOP_CHOPPER_DISTANCE / 2) && chopperMoving) ||
		   (transform.position.x > chopper.transform.position.x + SWOOP_CHOPPER_DISTANCE && !chopperMoving)) {
                isHeadingRight = !isHeadingRight;   // Change direction
                Swoop();
        }

        // If we end up right of the river, take off (should only happen while hunting)
        if (transform.position.x > enemyTerritoryBoundary) Retreat();
    }

    void Roll() {
        rollTime += Time.deltaTime;

        if (rollTime >= TIME_TO_ROLL)
            // TODO: understand why -1.5f works
            transform.Rotate(Vector3.forward, -1.5f, Space.Self);
        else {
            roll = Time.deltaTime / TIME_TO_ROLL * (initialRoll - FINAL_ROLL_ANGLE);
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
        swoopRotation = Time.deltaTime * (FINAL_SWOOP_ANGLE - initialPitch) / TIME_TO_SWOOP;

        if (swoopTime >= TIME_TO_SWOOP) {
            // FIXME: this doesn't work; need to fiddle with FINAL_SWOOP_ANGLE and possibly swoopRotation formula
//              transform.Rotate(Vector3.up, FINAL_SWOOP_ANGLE, Space.World);
            Attack();
        }
        else transform.Rotate(Vector3.up, swoopRotation, Space.World);

        Roll();
    }

    void Attack() {
        // Fire; detach missiles from jet, enable colliders and gravity, and add force
        // TODO: loop through the missiles
        missileLeft.transform.parent = null;
        missileLeft.GetComponent<Rigidbody>().useGravity = true;
        // TODO: missile trajectory needs more work. Play with gravity/downward force/drag
        missileLeft.GetComponent<Rigidbody>().AddForce(Vector3.left * missileSpeed, ForceMode.VelocityChange);
        missileLeft.GetComponent<Rigidbody>().AddForce(Vector3.down * 250, ForceMode.Acceleration);
        missileLeft.GetComponent<Rigidbody>().detectCollisions = true;
        missileLeft.transform.Find("MissileExhaust").gameObject.SetActive(true);   // Enable exhaust effect

        missileRight.transform.parent = null;
        missileRight.GetComponent<Rigidbody>().useGravity = true;
        missileRight.GetComponent<Rigidbody>().AddForce(Vector3.left * missileSpeed, ForceMode.VelocityChange);
        missileRight.GetComponent<Rigidbody>().AddForce(Vector3.down * 250, ForceMode.Acceleration);
        missileRight.GetComponent<Rigidbody>().detectCollisions = true;
        missileRight.transform.Find("MissileExhaust").gameObject.SetActive(true);

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
            psMainLeftExhaust.startSizeMultiplier = 5;  // Intensify jets
            psMainRightExhaust.startSizeMultiplier = 5;
        }

        retreatTime += Time.deltaTime;
        speed += retreatTime * RETREAT_SPEED_FACTOR;

        // Increase pitch of engine as we retreat. If we were using forces to move jet, Doppler Level would do this
        engineSound.pitch += retreatTime * RETREAT_PITCH_FACTOR;

        // Note that this will be true if the Scene camera can see it, so the jet may stay around longer in dev
        if (Mathf.Abs(transform.position.x - chopper.transform.position.x) > 100 && !GetComponent<Renderer>().isVisible)
            Destroy(gameObject, 1);
    }

    void OnCollisionEnter(Collision col) {
        // Wide collider on jet makes it too easy to collide; don't collide before attack
        if (col.gameObject.tag == "chopper" && !isHunting && !isSwooping) {
            GameObject expClone = GameObject.Instantiate(explosion, col.gameObject.transform.position,
                Quaternion.identity);
            Destroy(expClone, 3);   // Explosion lasts 3 secs
            Destroy(gameObject);
            col.gameObject.GetComponent<ControlChopper>().Crash();
        }
    }
}
