﻿/* nachtmission
 * Copyright (c) 2017-2018 Erik Niklas
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManageUI : MonoBehaviour {
    private const int     NUM_CHOPPERS = 3;             // Total lives
    private const float   CREDITS_SCROLL_SPEED = 80;    // Speed that credits scroll up the screen
    private const float   CREDITS_EXIT_FACTOR = 1.6f;   // Stop credits when they reach this factor of screen height
    private Vector3       creditsStartPosition;         // Position of the credits (bottom of screen)
    private bool          isScrollingCredits = false;   // True if the credits are being shown
    public int            numChoppersLeft;              // Lives left
    private int           prisonersTotal;               // Total number of prisoners to rescue
    private int           prisonersCaptive;             // Number of prisoners to rescue
    public int            prisonersRescued = 0;         // Number returned to base
    public int            prisonersOnboard = 0;         // Number on board the chopper
    public int            prisonersKilled = 0;          // Number killed
    private int           prisonersPerPrison;
    private GameObject    scoreText;
    private GameObject    gameOverText;
    private GameObject    instructionText;
    private GameObject    howToPlayText;
    private GameObject    creditsText;
    private GameObject    versionText;
    private GameObject    chopper;
    private ManageEnemies enemyScript;
    private ManageCamera  cameraScript;
    private ManageChopper chopperScript;
    public  bool          gameOver = false;
    private float         blinkFreq = 1.0f;              // How fast text blinks in secs
    private float         timeSinceBlink = 0.0f;
    private const float   TANK_SPAWN_PERCENTAGE = 0.0f;  // Start spawning tanks this far into the game
    private const float   JET_SPAWN_PERCENTAGE = 0.25f;  // Start spawning jets this far into the game
    private const float   DRONE_SPAWN_PERCENTAGE = 0.5f; // Start spawning drones this far into the game
    private bool          tanksActive = false;
    private bool          jetsActive = false;
    private bool          dronesActive = false;
    private bool          isShowingMenu = true;          // True if the game is paused/showing menu
    private bool          isShowingHowToPlay = false;    // True if we're displaying the How to Play text
    public  bool          firstGameLaunch = true;        // False if the user has clicked New Game
    private GameObject    resumeGameButton;
    private GameObject    menu;
    private GameObject    terrain;
    private GameObject[]  terrains;
    public  GameObject    prisonDamaged;
    public  float         timeSinceMenuClear = 0.0f;     // Time since menu has disappeared (to prevent unwanted firing)

	void Awake () {
        numChoppersLeft = NUM_CHOPPERS;
        scoreText = transform.Find("ScoreText").gameObject;
        gameOverText = transform.Find("GameOverText").gameObject;
        instructionText = transform.Find("InstructionText").gameObject;
        howToPlayText = transform.Find("HowToPlayText").gameObject;
        creditsText = transform.Find("CreditsText").gameObject;
        creditsStartPosition = creditsText.GetComponent<RectTransform>().transform.position;
        versionText = transform.Find("VersionText").gameObject;
        menu = transform.Find("Menu").gameObject;
        resumeGameButton = transform.Find("Menu/ResumeGameButton").gameObject;
        resumeGameButton.SetActive(false);
    }

	void Start () {
        chopper = GameObject.Find("Chopper");
        chopperScript = chopper.GetComponent<ManageChopper>();
        cameraScript = GameObject.Find("Main Camera").GetComponent<ManageCamera>();
        enemyScript = GameObject.Find("Enemies").GetComponent<ManageEnemies>();

        // Hack to determine if this is the first time through the game, or if user has clicked New Game; this is
        //  necessary because we want to show the menu at game launch, but not after clicking New Game
        terrains = GameObject.FindGameObjectsWithTag("terrain");

        if (terrains.Length > 1) {
            Destroy(terrains[1]);
            firstGameLaunch = false;
            // Only show title on first launch
            GameObject.Find("Title").SetActive(false);
            MenuToggle();
        }
        else {
            Pause(true);
            cameraScript.titleZoomIn = false;
            cameraScript.ZoomTitle();
        }

        terrain = GameObject.Find("Terrain");
        DontDestroyOnLoad(terrain);

        // Get number of prisoners
        prisonersPerPrison = prisonDamaged.GetComponent<GeneratePrisoners>().prisonersRemaining;
        prisonersTotal = prisonersPerPrison * GameObject.FindGameObjectsWithTag("prison").Length;
        prisonersCaptive = prisonersTotal;

        // Populate prisoner count and chopper capacity in instruction text
        howToPlayText.GetComponent<Text>().text =
            howToPlayText.GetComponent<Text>().text.Replace("NUM_PRISONERS", prisonersPerPrison.ToString());
        howToPlayText.GetComponent<Text>().text =
            howToPlayText.GetComponent<Text>().text.Replace("CAPACITY", chopperScript.capacity.ToString());

        UpdateScore();
	}

    public void ResumeGameButton() {
        MenuToggle();
    }

    public void NewGameButton() {
        if (!firstGameLaunch) SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        else {
            MenuToggle();
            firstGameLaunch = false;
        }
    }

    public void HowToPlayButton() {
        isShowingHowToPlay = true;
        menu.SetActive(false);
        howToPlayText.SetActive(true);
        timeSinceMenuClear = 0.0f;
    }

    public void CreditsButton() {
        ScrollCredits();
    }

    public void ExitGameButton() {
        Application.Quit();
    }

    void Pause(bool pause) {
        if (pause) {
            Time.timeScale = 0.0f;
            AudioListener.pause = true;
        }
        else {
            Time.timeScale = 1.0f;
            AudioListener.pause = false;
        }
    }

    void MenuToggle() {
        if (!isShowingMenu) {
            Pause(true);
            menu.SetActive(true);
            versionText.SetActive(true);

            // Show resume game button if game is in progress
            if (gameOver || firstGameLaunch)
                resumeGameButton.SetActive(false);
            else resumeGameButton.SetActive(true);
        }

        else {
            Pause(false);
            timeSinceMenuClear = 0.0f;
            menu.SetActive(false);
            versionText.SetActive(false);

            if (firstGameLaunch) {
                // Needed in case user starts first game with Start button
                firstGameLaunch = false;

                // Smoothly reverse title zoom if player starts before zoom out is done
                if (cameraScript.titleZoomTime > 0 && cameraScript.titleZoomTime < cameraScript.titleZoomDuration)
                    cameraScript.titleZoomTime = cameraScript.titleZoomDuration - cameraScript.titleZoomTime;

                cameraScript.titleZoomIn = true;
                cameraScript.ZoomTitle();
            }
        }

        isShowingMenu = !isShowingMenu;
    }

    void ReturnToMenuFromTextScreen() {
        howToPlayText.SetActive(false);
        creditsText.SetActive(false);
        isShowingHowToPlay = false;
        isScrollingCredits = false;
        scoreText.SetActive(true);
        menu.SetActive(true);
        Pause(true);
    }

    void ScrollCredits() {
        if (!isScrollingCredits) {          // First time through
            isScrollingCredits = true;
            menu.SetActive(false);
            scoreText.SetActive(false);     // Could get in the way on lower resolutions
            creditsText.SetActive(true);
            creditsText.GetComponent<RectTransform>().transform.position = creditsStartPosition;
            timeSinceMenuClear = 0.0f;
        }

        else {
            creditsText.transform.Translate(Vector3.up * CREDITS_SCROLL_SPEED * Time.unscaledDeltaTime);
            if (creditsText.transform.position.y > Screen.height * CREDITS_EXIT_FACTOR) ReturnToMenuFromTextScreen();
        }
    }

    void Update() {
        if (isShowingHowToPlay || isScrollingCredits) {
            if (Input.anyKeyDown && timeSinceMenuClear > 0.2f) ReturnToMenuFromTextScreen();
            else if (isScrollingCredits) ScrollCredits();
        }

        // Esc or Start to pause/show menu
        else if (Input.GetKeyDown(KeyCode.Escape) || (Input.GetKeyDown(KeyCode.JoystickButton7))) MenuToggle();

        else if (gameOver) {
            if (Input.anyKeyDown) NewGameButton();
            // Blink instruction text
            timeSinceBlink += Time.deltaTime;

            if (timeSinceBlink >= blinkFreq) {
                instructionText.SetActive(!instructionText.activeSelf);
                timeSinceBlink = 0.0f;
            }
        }

        timeSinceMenuClear += Time.unscaledDeltaTime;
    }

    // This is called by ManageChopper when a chopper crash is complete
    public void chopperDestroyed() {
        numChoppersLeft--;

        if (prisonersOnboard > 0) {
            prisonersKilled += prisonersOnboard;
            prisonersOnboard = 0;
        }

        UpdateScore();

        if (numChoppersLeft == 0) GameOver();
        else {
            chopperScript.initChopper();
            // Destroy any leftover bullets to avoid seeing them pass by on the next life
            foreach (GameObject bullet in GameObject.FindGameObjectsWithTag("bullet"))
                Destroy(bullet);
        }
    }

    void GameOver() {
        gameOver = true;
        // Disable rotors and chopper control
        chopperScript.enabled = false;
        chopper.transform.Find("ChopperRotor").GetComponent<ManageRotor>().enabled = false;
        chopper.transform.Find("ChopperTailRotor").GetComponent<ManageRotor>().enabled = false;

        if (prisonersRescued == prisonersTotal) {
            // Player rescued all prisoners without losing any choppers
            if (numChoppersLeft == NUM_CHOPPERS) gameOverText.GetComponent<Text>().text = ("Perfect!");
            // Player rescued all prisoners but died at least once
            else gameOverText.GetComponent<Text>().text = ("Excellent!");
        }
        else gameOverText.GetComponent<Text>().text = ("Game Over");

        instructionText.GetComponent<Text>().text = ("Press any button to replay");
    }

    public void UpdateScore() {
        prisonersCaptive = prisonersTotal - prisonersOnboard - prisonersRescued - prisonersKilled;
        scoreText.GetComponent<Text>().text = ("Lives: " + numChoppersLeft +
                                               "\nCaptive: " + prisonersCaptive +
                                               "\nOn board: " + prisonersOnboard +
                                               "\nRescued: " + prisonersRescued +
                                               "\nKilled: " + prisonersKilled);

        if (prisonersRescued + prisonersKilled == prisonersTotal) GameOver();

        // Spawn additional enemies later in the game
        if (!tanksActive &&
            (float) (prisonersRescued + prisonersKilled) >= (float) prisonersTotal * TANK_SPAWN_PERCENTAGE) {
                tanksActive = true;
                enemyScript.IntroduceTanks();
        }

        if (!jetsActive &&
            (float) (prisonersRescued + prisonersKilled) >= (float) prisonersTotal * JET_SPAWN_PERCENTAGE) {
                jetsActive = true;
                enemyScript.IntroduceJets();
        }

        if (!dronesActive &&
            (float) (prisonersRescued + prisonersKilled) >= (float) prisonersTotal * DRONE_SPAWN_PERCENTAGE) {
                dronesActive = true;
                enemyScript.IntroduceDrones();
        }
    }
}
