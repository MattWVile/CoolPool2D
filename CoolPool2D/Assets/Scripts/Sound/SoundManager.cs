using UnityEngine;

public class SoundManager : MonoBehaviour
{

    public AudioClip BallHitClip;
    public AudioClip BallHit2Clip;
    public AudioClip CueHitClip;
    public AudioClip RailHitClip;

    public AudioClip ClickClip;

    private AudioSource audioSource;


    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();


        EventBus.Subscribe<BallHasBeenShotEvent>((@event => PlaySound(CueHitClip)));
        EventBus.Subscribe<BallCollidedWithRailEvent>((@event =>
        {
            var velocity = @event.BallData.gameObject.GetComponent<DeterministicBall>().velocity.magnitude;
            velocity = velocity / 20; // normalize based on expected max speed
            velocity = Mathf.Clamp(velocity, 0.2f, 1f);
            PlaySound(RailHitClip, velocity);
        }));
        EventBus.Subscribe<BallKissedEvent>((@event =>
        {
            var velocity = @event.BallData.gameObject.GetComponent<DeterministicBall>().velocity.magnitude;
            velocity = velocity / 30; // normalize based on expected max speed
            velocity = Mathf.Clamp(velocity, 0.8f, 1.2f);
            PlaySound(BallHitClip, velocity, velocity);
        }));

        EventBus.Subscribe<DisplayMultiplierPopUpEvent>((@event => {
            var pitch = 1f + (0.1f * (@event.MultiplierCount));
            PlaySound(ClickClip, 0.7f, pitch);
        }));


    }
    void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        audioSource.pitch = pitch; 
        audioSource.volume = volume; 
        audioSource.PlayOneShot(clip);

    }

}