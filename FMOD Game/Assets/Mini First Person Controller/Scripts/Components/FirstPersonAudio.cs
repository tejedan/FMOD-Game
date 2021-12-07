using System.Linq;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FirstPersonAudio : MonoBehaviour
{
    public FirstPersonMovement character;
    public GroundCheck groundCheck;

    [Header("Step")]
    public AudioSource stepAudio;
    public AudioSource runningAudio;
    [Tooltip("Minimum velocity for moving audio to play")]
    /// <summary> "Minimum velocity for moving audio to play" </summary>
    public float velocityThreshold = .01f;
    Vector2 lastCharacterPosition;
    Vector2 CurrentCharacterPosition => new Vector2(character.transform.position.x, character.transform.position.z);

    [Header("Landing")]
    public AudioSource landingAudio;
    public AudioClip[] landingSFX;

    [Header("Jump")]
    public Jump jump;
    public AudioSource jumpAudio;
    public AudioClip[] jumpSFX;

    [Header("Crouch")]
    public Crouch crouch;
    public AudioSource crouchStartAudio, crouchedAudio, crouchEndAudio;
    public AudioClip[] crouchStartSFX, crouchEndSFX;

    private string EventPath = "event:/footsteps";

    AudioSource[] MovingAudios => new AudioSource[] { stepAudio, runningAudio, crouchedAudio };

    private void Awake()
    {
        var runEmitter = this.transform.GetChild(1).GetComponent<FMODUnity.StudioEventEmitter>();
        runEmitter.Stop();
    }
    
    void Reset()
    {
        // Setup stuff.
        character = GetComponentInParent<FirstPersonMovement>();
        groundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();

        // stepAudio = GetOrCreateAudioSource("Step Audio");
        landingAudio = GetOrCreateAudioSource("Landing Audio");

        // Setup jump audio.
        jump = GetComponentInParent<Jump>();
        if (jump)
        {
            jumpAudio = GetOrCreateAudioSource("Jump audio");
        }

        // Setup crouch audio.
        crouch = GetComponentInParent<Crouch>();
        if (crouch)
        {
            crouchStartAudio = GetOrCreateAudioSource("Crouch Start Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouched Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouch End Audio");
        }
    }

    void OnEnable() => SubscribeToEvents();

    void OnDisable() => UnsubscribeToEvents();

    void FixedUpdate()
    {
        // Play moving audio if the character is moving and on the ground.
        float velocity = Vector3.Distance(CurrentCharacterPosition, lastCharacterPosition);

        if (velocity >= velocityThreshold && groundCheck && groundCheck.isGrounded)
        {
            if (crouch && crouch.IsCrouched)
            {
                SetPlayingMovingAudio(crouchedAudio);
            }
            else if (character.IsRunning)
            {
                SetPlayingMovingAudio(runningAudio);
            }
            else
            {
                SetPlayingMovingAudio(stepAudio);
            }
        }
        else
        {
            SetPlayingMovingAudio(null);
        }

        // Remember lastCharacterPosition.
        lastCharacterPosition = CurrentCharacterPosition;
    }


    /// <summary>
    /// Pause all MovingAudios and enforce play on audioToPlay.
    /// </summary>
    /// <param name="audioToPlay">Audio that should be playing.</param>
    void SetPlayingMovingAudio(AudioSource audioToPlay)
    {
        // Pause all MovingAudios.
        foreach (var audio in MovingAudios.Where(audio => audio != audioToPlay && audio != null))
        {
            audio.Pause();
        }

        // Play audioToPlay if it was not playing.
        if (audioToPlay && !audioToPlay.isPlaying)
        {
            audioToPlay.Play();
        }
    }

    void PlayRunEvent()
    {
        // Start with material check then instantiate sound
        EventInstance Run = RuntimeManager.CreateInstance(EventPath);
        RuntimeManager.AttachInstanceToGameObject(Run, transform, GetComponent<Rigidbody>());

        // Sets the 'WalkRun' parameter
        Run.setParameterByName("WalkRun", 1, false);

        Run.start();
        Run.release();
    }

    void PlayWalkEvent()
    {
        EventInstance Walk = RuntimeManager.CreateInstance(EventPath);
        RuntimeManager.AttachInstanceToGameObject(Walk, transform, GetComponent<Rigidbody>());

        // Sets the Terrain parameter
        //Walk.setParameterByID(ParamID, MaterialValue, false);

        // Sets the 'WalkRun' parameter
        Walk.setParameterByName("WalkRun", 0, false);

        // Can be used as alternative to IDs
        // Run.setParameterByName("Terrain", MaterialValue);

        Walk.start();
        Walk.release();
    }

    #region Play instant-related audios.
    void PlayLandingAudio() => PlayRandomClip(landingAudio, landingSFX);
    void PlayJumpAudio() => PlayRandomClip(jumpAudio, jumpSFX);
    void PlayCrouchStartAudio() => PlayRandomClip(crouchStartAudio, crouchStartSFX);
    void PlayCrouchEndAudio() => PlayRandomClip(crouchEndAudio, crouchEndSFX);
    #endregion

    #region Subscribe/unsubscribe to events.
    void SubscribeToEvents()
    {
        // PlayLandingAudio when Grounded.
        groundCheck.Grounded += PlayLandingAudio;

        // PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped += PlayJumpAudio;
        }

        // Play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart += PlayCrouchStartAudio;
            crouch.CrouchEnd += PlayCrouchEndAudio;
        }
    }

    void UnsubscribeToEvents()
    {
        // Undo PlayLandingAudio when Grounded.
        groundCheck.Grounded -= PlayLandingAudio;

        // Undo PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped -= PlayJumpAudio;
        }

        // Undo play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart -= PlayCrouchStartAudio;
            crouch.CrouchEnd -= PlayCrouchEndAudio;
        }
    }
    #endregion

    #region Utility.
    /// <summary>
    /// Get an existing AudioSource from a name or create one if it was not found.
    /// </summary>
    /// <param name="name">Name of the AudioSource to search for.</param>
    /// <returns>The created AudioSource.</returns>
    AudioSource GetOrCreateAudioSource(string name)
    {
        // Try to get the audiosource.
        AudioSource result = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (result)
            return result;

        // Audiosource does not exist, create it.
        result = new GameObject(name).AddComponent<AudioSource>();
        result.spatialBlend = 1;
        result.playOnAwake = false;
        result.transform.SetParent(transform, false);
        return result;
    }

    static void PlayRandomClip(AudioSource audio, AudioClip[] clips)
    {
        if (!audio || clips.Length <= 0)
            return;

        // Get a random clip. If possible, make sure that it's not the same as the clip that is already on the audiosource.
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
            while (clip == audio.clip)
                clip = clips[Random.Range(0, clips.Length)];

        // Play the clip.
        audio.clip = clip;
        audio.Play();
    }
    #endregion 
}
