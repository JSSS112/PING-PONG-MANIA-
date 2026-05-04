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

    // Lógica de rebotes para puntuación correcta de ping pong
    private enum LadoRebote { Ninguno, Jugador, Jefe }
    private LadoRebote ultimoRebote = LadoRebote.Ninguno;

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Asegurar que exista un VRLaserPointer para interactuar con el panel
        // de resultado (botón "Jugar de nuevo") al final de la partida.
        if (FindFirstObjectByType<VRLaserPointer>() == null)
        {
            GameObject go = new GameObject("LaserPointer");
            go.AddComponent<VRLaserPointer>();
        }
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
        ultimoRebote = LadoRebote.Ninguno;

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

    // ════════════════════════════════════════════════════════════════════════
    // REBOTES Y GOLPES — lógica simple de ping pong
    // ════════════════════════════════════════════════════════════════════════
    // Regla: si la pelota rebota 2 veces seguidas en el mismo lado sin que ese
    // jugador la golpee, ese lado pierde el punto. Cuando la raqueta del jugador
    // o el jefe golpean, se reinicia el contador.
    public void RegistrarRebote(bool ladoJefe)
    {
        if (!roundActive || gameOver) return;

        LadoRebote nuevoLado = ladoJefe ? LadoRebote.Jefe : LadoRebote.Jugador;
        Debug.Log($"[OASIS] Rebote en: {(ladoJefe ? "LADO JEFE" : "LADO JUGADOR")} | anterior={ultimoRebote}");

        if (ultimoRebote == nuevoLado)
        {
            // 2do rebote consecutivo en el mismo lado → ese lado falló
            ultimoRebote = LadoRebote.Ninguno;
            if (ladoJefe)
            {
                Debug.Log("[OASIS] Doble rebote lado JEFE → punto JUGADOR");
                JugadorAnota();
            }
            else
            {
                Debug.Log("[OASIS] Doble rebote lado JUGADOR → punto JEFE");
                JefeAnota();
            }
            return;
        }

        ultimoRebote = nuevoLado;
        BallWatchdog.instance?.RegistrarGolpe();
    }

    // Llamar desde RaquetaJugador y BossAI cuando golpean la pelota.
    public void RegistrarGolpeRaqueta()
    {
        ultimoRebote = LadoRebote.Ninguno;
        BallWatchdog.instance?.RegistrarGolpe();
    }
}