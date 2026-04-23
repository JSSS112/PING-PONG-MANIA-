using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Vidas")]
    public int bossLife   = 11;
    public int playerLife = 11;

    [Header("UI — Countdown y Resultado")]
    public TextMeshProUGUI countdownText;
    public GameObject      resultPanel;
    public TextMeshProUGUI resultText;

    [Header("UI — Sliders de vida")]
    public Slider bossLifeSlider;
    public Slider playerLifeSlider;

    [Header("UI — Textos de corazones")]
    public TextMeshProUGUI bossLifeText;
    public TextMeshProUGUI playerLifeText;

    [Header("OBLIGATORIO")]
    public BallSpawner ballSpawner;

    // Estado publico
    [HideInInspector] public bool roundActive = false;
    [HideInInspector] public bool gameOver    = false;

    // Saque alternado por numero de ronda: par = jugador, impar = jefe
    private int numeroRonda = 0;

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (resultPanel   != null) resultPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // Inicializar sliders y textos con valores completos
        ActualizarUI();

        numeroRonda = 0;
        StartCoroutine(IniciarRonda());
    }

    // ════════════════════════════════════════════════════════════════════════
    // ACTUALIZAR TODA LA UI DE VIDAS
    // ════════════════════════════════════════════════════════════════════════
    void ActualizarUI()
    {
        // Sliders
        if (bossLifeSlider   != null)
        {
            bossLifeSlider.minValue = 0;
            bossLifeSlider.maxValue = 11;
            bossLifeSlider.value    = bossLife;
        }
        if (playerLifeSlider != null)
        {
            playerLifeSlider.minValue = 0;
            playerLifeSlider.maxValue = 11;
            playerLifeSlider.value    = playerLife;
        }

        // Textos con corazones
        if (bossLifeText   != null)
            bossLifeText.text   = "Jefe " + new string('\u2665', bossLife)
                                          + new string('\u2661', 11 - bossLife);
        if (playerLifeText != null)
            playerLifeText.text = "Tu   " + new string('\u2665', playerLife)
                                          + new string('\u2661', 11 - playerLife);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PUNTUACION
    // ════════════════════════════════════════════════════════════════════════
    public void JefeAnota()
    {
        if (gameOver || !roundActive) return;
        roundActive = false;

        BallWatchdog.instance?.DetenerMonitoreo();
        ballSpawner?.DestruirPelotaActual();

        playerLife = Mathf.Max(0, playerLife - 1);
        ActualizarUI();

        Debug.Log($"[OASIS] JEFE anota! Jugador vida:{playerLife} | Jefe vida:{bossLife}");

        if (VerificarFinJuego()) return;

        numeroRonda++;
        StartCoroutine(EsperarYReiniciar());
    }

    public void JugadorAnota()
    {
        if (gameOver || !roundActive) return;
        roundActive = false;

        BallWatchdog.instance?.DetenerMonitoreo();
        ballSpawner?.DestruirPelotaActual();

        bossLife = Mathf.Max(0, bossLife - 1);
        ActualizarUI();

        Debug.Log($"[OASIS] JUGADOR anota! Jefe vida:{bossLife} | Jugador vida:{playerLife}");

        if (VerificarFinJuego()) return;

        numeroRonda++;
        StartCoroutine(EsperarYReiniciar());
    }

    // ════════════════════════════════════════════════════════════════════════
    // FLUJO DE RONDA
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator EsperarYReiniciar()
    {
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(IniciarRonda());
    }

    IEnumerator IniciarRonda()
    {
        bool jefeSaca = (numeroRonda % 2 != 0);

        Debug.Log($"[OASIS] Ronda {numeroRonda} | Saca: {(jefeSaca ? "JEFE" : "JUGADOR")}");

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            for (int i = 3; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(0.85f);
            }
            countdownText.text = jefeSaca ? "JEFE!" : "TU!";
            yield return new WaitForSeconds(0.5f);
            countdownText.gameObject.SetActive(false);
        }

        roundActive = true;

        if (ballSpawner != null)
        {
            ballSpawner.SpawnBall(jefeSaca);
            StartCoroutine(IniciarWatchdogConDelay(2.5f));
        }
        else
        {
            Debug.LogError("[OASIS] BallSpawner no asignado!");
        }
    }

    IEnumerator IniciarWatchdogConDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (roundActive) BallWatchdog.instance?.IniciarMonitoreo();
    }

    // ════════════════════════════════════════════════════════════════════════
    // FIN DE JUEGO
    // ════════════════════════════════════════════════════════════════════════
    bool VerificarFinJuego()
    {
        if (bossLife <= 0)
        {
            gameOver = true;
            FinJuego("VICTORIA!\nDerroto al jefe!");
            return true;
        }
        if (playerLife <= 0)
        {
            gameOver = true;
            FinJuego("DERROTA\nEl jefe te vencio.");
            return true;
        }
        return false;
    }

    void FinJuego(string msg)
    {
        Debug.Log("[OASIS] ========================");
        Debug.Log("[OASIS] " + msg);
        Debug.Log("[OASIS] ========================");

        Time.timeScale = 0f;

        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText  != null) resultText.text = msg;
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Rebote en la mesa
    public void RegistrarRebote(bool ladoJefe)
    {
        if (!roundActive) return;
        Debug.Log($"[OASIS] Rebote en: {(ladoJefe ? "LADO JEFE" : "LADO JUGADOR")}");
        BallWatchdog.instance?.RegistrarGolpe();
    }
}