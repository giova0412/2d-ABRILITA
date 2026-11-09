using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _effectsSource;
    [Header("Archivos de audio")]
    [SerializeField] private AudioClip _backgroundMusic;
    [SerializeField] private AudioClip _coinSound;
    [SerializeField] private AudioClip _jumpSound;
    [SerializeField] private AudioClip _deathSound;

    private void Awake()
    {
        // Conservamos la primera instancia para no cortar la música al reiniciar.
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (_musicSource == null || _effectsSource == null || _musicSource == _effectsSource)
        {
            Debug.LogError("Asigna dos AudioSource diferentes al AudioManager.", this);
            return;
        }
        _musicSource.playOnAwake = false;
        _musicSource.spatialBlend = 0f;
        _musicSource.loop = true;
        _musicSource.clip = _backgroundMusic;
        _effectsSource.playOnAwake = false;
        _effectsSource.spatialBlend = 0f;
        _effectsSource.loop = false;
        if (_backgroundMusic != null) _musicSource.Play();
    }

    public void PlayCoin() { PlayEffect(_coinSound); }
    public void PlayJump() { PlayEffect(_jumpSound); }
    public void PlayDeath() { PlayEffect(_deathSound); }

    private void PlayEffect(AudioClip clip)
    {
        // El efecto no desaparece al destruir la moneda ni al cambiar de escena.
        if (_effectsSource != null && clip != null) _effectsSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
