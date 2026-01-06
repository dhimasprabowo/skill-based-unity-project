using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance;

	[Header("Audio Source")]
	public AudioSource sfxSource;

	[Header("Sounds")]
	public AudioClip flipSound;
	public AudioClip matchSuccessSound;
	public AudioClip matchFailSound;
	public AudioClip gameCompleteSound;

	public bool allowFlipSound = true;


	void Awake()
	{
		// Singleton pattern
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (sfxSource == null)
		{
			sfxSource = gameObject.AddComponent<AudioSource>();
			sfxSource.playOnAwake = false;
		}
	}

	// ================= PUBLIC API =================

	public void PlayFlip()
	{
		if (!allowFlipSound)
			return;

		PlayOneShot(flipSound);
	}


	public void PlayMatchSuccess()
	{
		PlayOneShot(matchSuccessSound);
	}

	public void PlayMatchFail()
	{
		PlayOneShot(matchFailSound);
	}

	public void PlayGameComplete()
	{
		PlayOneShot(gameCompleteSound);
	}

	// ================= INTERNAL =================

	void PlayOneShot(AudioClip clip)
	{
		if (clip == null || sfxSource == null)
			return;

		sfxSource.PlayOneShot(clip);
	}
}
