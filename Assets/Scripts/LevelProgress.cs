using UnityEngine;

public class LevelProgress : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private TMPro.TextMeshProUGUI livesText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;
    [SerializeField] private TMPro.TextMeshProUGUI resultText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private int totalCoins;
    private UnityEngine.InputSystem.InputAction restartAction;
    public int Coins { get; private set; }
    public bool Finished { get; private set; }

    private void Awake()
    {
        restartAction = new UnityEngine.InputSystem.InputAction("Restart", UnityEngine.InputSystem.InputActionType.Button);
        UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(restartAction, "<Keyboard>/r");
        UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(restartAction, "<Gamepad>/start");
    }

    private void OnEnable()
    {
        player.LivesChanged += RefreshLives;
        player.GameOver += ShowGameOver;
        restartAction.performed += OnRestart;
        restartAction.Enable();
    }

    private void Start()
    {
        resultPanel.SetActive(false);
        RefreshLives(player.Lives);
        RefreshCoins();
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.LivesChanged -= RefreshLives;
            player.GameOver -= ShowGameOver;
        }
        restartAction.performed -= OnRestart;
        restartAction.Disable();
    }

    private void OnDestroy() { restartAction.Dispose(); }

    private void RefreshLives(int lives) { livesText.SetText("VIDAS: {0}", (float)lives); }
    private void RefreshCoins() { coinsText.SetText("MONEDAS: {0} / {1}", (float)Coins, (float)totalCoins); }

    public bool CollectCoin(PlayerController collector)
    {
        if (collector != player || player.IsDead || Finished) return false;
        Coins++;
        RefreshCoins();
        return true;
    }

    public void ReachFinish(PlayerController collector)
    {
        if (collector != player || player.IsDead || Finished) return;
        Finished = true;
        player.CompleteLevel();
        resultText.text = "¡NIVEL COMPLETADO!\n\nMonedas: " + Coins + " / " + totalCoins + "\n\nPulsa R para volver a jugar";
        resultPanel.SetActive(true);
    }

    private void ShowGameOver()
    {
        Finished = true;
        resultText.text = "SIN VIDAS\n\nPulsa R para reiniciar";
        resultPanel.SetActive(true);
    }

    private void OnRestart(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (Finished) UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
