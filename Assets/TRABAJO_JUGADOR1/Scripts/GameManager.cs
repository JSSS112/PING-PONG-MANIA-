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

    [Header("Saque del jugador (opcional pero recomendado)")]
    public SistemaDeServicio sistemaDeServicio;

    [Header("UI — Feedback de punto (encima del jefe)")]
    [Tooltip("Texto grande que muestra '+1 PUNTO!' / 'PERDISTE -1' tras cada punto.")]
    public TextMeshProUGUI mensajePuntoText;
    [Tooltip("Cuanto dura el mensaje en pantalla.")]
    public float duracionMensajePunto = 1.8f;

    [Header("UI — Nombre del jefe")]
    [Tooltip("Texto donde se renderiza el nombre malvado del jefe.")]
    public TextMeshProUGUI nombreJefeText;
    [Tooltip("Nombre que se muestra encima del jefe.")]
    public string nombreJefe = "ALEJANDRO EL TIRANO";

    [Header("Audio — musica de fondo (opcional)")]
    [Tooltip("AudioSource con tu cancion relajante. Arranca con el juego, para cuando termina.")]
    public AudioSource musicaFondo;

    [Header("UI — Posicion del panel de resultado en VR")]
    [Tooltip("Si esta en true, el panel se reposiciona frente al jugador al ganar/perder.")]
    public bool reposicionarPanelResultado = true;
    [Tooltip("Distancia en metros desde el jugador al panel de resultado.")]
    public float distanciaPanelResultado = 2f;
    [Tooltip("Altura relativa al ojo del jugador.")]
    public float alturaPanelResultado = 0.0f;

    // Estado publico
    [HideInInspector] public bool roundActive = false;
    [HideInInspector] public bool gameOver    = false;

    // Cuanta vida quita el punto actual. PelotaBehaviour lo setea a 2 si la
    // pelota spawneada es AMARILLA, sino queda en 1. Se resetea tras anotar.
    [HideInInspector] public int  puntoVale   = 1;

    // Saque alternado por numero de ronda: par = jugador, impar = jefe
    private int numeroRonda = 0;

    // ── ESTADO DEL RALLY ─────────────────────────────────────────────────────
    // Regla simple de ping pong:
    //   - Quien acaba de golpear (jefe / jugador) define a quien le toca:
    //     la pelota debe rebotar en el lado OPUESTO al golpeador.
    //   - Si rebota en el mismo lado del golpeador  → punto al oponente
    //     (la mando a su propio campo).
    //   - Si rebota 2 veces en lado del oponente sin que el oponente la
    //     devuelva → punto al golpeador (el oponente no la devolvio).
    //   - Si la pelota se pierde con 0 botes en lado del oponente → punto
    //     al oponente (la mandaron afuera).
    //   - Si se pierde con 1+ botes en lado del oponente → punto al
    //     golpeador (el oponente no la devolvio).
    private enum Lado { Ninguno, Jugador, Jefe }
    private Lado ultimoGolpeador = Lado.Ninguno;
    private int  botesEnLadoOponente = 0;

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
        if (mensajePuntoText != null) mensajePuntoText.gameObject.SetActive(false);

        // Pintar el nombre del jefe (si se asigno el texto).
        if (nombreJefeText != null) nombreJefeText.text = nombreJefe;

        // Arrancar la musica de fondo si esta asignada.
        if (musicaFondo != null && musicaFondo.clip != null)
        {
            musicaFondo.loop = true;
            musicaFondo.Play();
        }

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
        sistemaDeServicio?.HabilitarSaque(false);

        int dano = Mathf.Max(1, puntoVale);
        playerLife = Mathf.Max(0, playerLife - dano);
        puntoVale = 1;            // reset para la proxima ronda
        ActualizarUI();

        Debug.Log($"[OASIS] JEFE anota (-{dano})! Jugador vida:{playerLife} | Jefe vida:{bossLife}");
        MostrarMensajePunto($"PERDISTE -{dano}", new Color(1f, 0.3f, 0.3f));

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
        sistemaDeServicio?.HabilitarSaque(false);

        int dano = Mathf.Max(1, puntoVale);
        bossLife = Mathf.Max(0, bossLife - dano);
        puntoVale = 1;            // reset para la proxima ronda
        ActualizarUI();

        Debug.Log($"[OASIS] JUGADOR anota (-{dano})! Jefe vida:{bossLife} | Jugador vida:{playerLife}");
        MostrarMensajePunto($"+{dano} PUNTO!", new Color(0.3f, 1f, 0.4f));

        if (VerificarFinJuego()) return;

        numeroRonda++;
        StartCoroutine(EsperarYReiniciar());
    }

    // ════════════════════════════════════════════════════════════════════════
    // FEEDBACK VISUAL — mensaje de punto encima del jefe
    // ════════════════════════════════════════════════════════════════════════
    void MostrarMensajePunto(string texto, Color color)
    {
        if (mensajePuntoText == null) return;
        StopCoroutine(nameof(OcultarMensajePunto));
        mensajePuntoText.text = texto;
        mensajePuntoText.color = color;
        mensajePuntoText.gameObject.SetActive(true);
        StartCoroutine(OcultarMensajePunto());
    }

    IEnumerator OcultarMensajePunto()
    {
        yield return new WaitForSecondsRealtime(duracionMensajePunto);
        if (mensajePuntoText != null) mensajePuntoText.gameObject.SetActive(false);
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
        ultimoGolpeador = Lado.Ninguno;
        botesEnLadoOponente = 0;

        // Mientras dura el countdown, el saque del jugador esta deshabilitado.
        sistemaDeServicio?.HabilitarSaque(false);

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
            // Solo habilitamos el saque del jugador si esta ronda le toca a el.
            sistemaDeServicio?.HabilitarSaque(!jefeSaca);
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

        // Parar musica de fondo si esta sonando.
        if (musicaFondo != null && musicaFondo.isPlaying) musicaFondo.Stop();

        // Ocultar UI residual.
        if (mensajePuntoText != null) mensajePuntoText.gameObject.SetActive(false);
        if (countdownText != null)   countdownText.gameObject.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (reposicionarPanelResultado) ReposicionarPanelResultado();
        }
        if (resultText != null) resultText.text = msg;
    }

    // Pone el panel de resultado 2 m frente al jugador, mirando hacia el.
    // Asi se ve bien sin importar donde esta posicionado en la jerarquia.
    void ReposicionarPanelResultado()
    {
        if (resultPanel == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 pos = cam.transform.position + forward * distanciaPanelResultado;
        pos.y = cam.transform.position.y + alturaPanelResultado;
        resultPanel.transform.position = pos;
        resultPanel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ════════════════════════════════════════════════════════════════════════
    // REBOTES Y GOLPES — regla de ping pong real
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lo llaman RaquetaJugador (false) y BossAI (true) cuando golpean / sacan.
    /// Resetea el contador de botes en lado oponente, porque el rally arranca
    /// de nuevo desde el nuevo golpeador.
    /// </summary>
    public void RegistrarGolpe(bool fueElJefe)
    {
        if (!roundActive || gameOver) return;
        ultimoGolpeador = fueElJefe ? Lado.Jefe : Lado.Jugador;
        botesEnLadoOponente = 0;
        BallWatchdog.instance?.RegistrarGolpe();
        Debug.Log($"[OASIS] Golpe registrado por {ultimoGolpeador}");
    }

    /// <summary>
    /// Lo llama TableBounce cuando la pelota rebota en una mitad de la mesa.
    /// Aplica las reglas para decidir punto / continuar rally.
    /// </summary>
    public void RegistrarRebote(bool ladoJefe)
    {
        if (!roundActive || gameOver) return;

        Lado ladoBote = ladoJefe ? Lado.Jefe : Lado.Jugador;
        BallWatchdog.instance?.RegistrarGolpe();

        // Si nadie golpeo todavia, tomamos al que sacaba como el "golpeador".
        if (ultimoGolpeador == Lado.Ninguno)
        {
            bool jefeSaca = (numeroRonda % 2 != 0);
            ultimoGolpeador = jefeSaca ? Lado.Jefe : Lado.Jugador;
            botesEnLadoOponente = 0;
        }

        Debug.Log($"[OASIS] Rebote en {ladoBote} | ultimoGolpeador={ultimoGolpeador} | botesEnLadoOponente={botesEnLadoOponente}");

        // Caso 1: rebota en el mismo lado del que la golpeo
        //   → la mando a su propio campo → punto al oponente
        if (ladoBote == ultimoGolpeador)
        {
            Debug.Log($"[OASIS] {ultimoGolpeador} la mando a su propio lado → punto al oponente");
            if (ultimoGolpeador == Lado.Jefe) JugadorAnota();
            else                              JefeAnota();
            return;
        }

        // Caso 2: rebota en el lado del oponente
        botesEnLadoOponente++;

        // Caso 2a: segundo bote en lado del oponente → oponente no devolvio
        if (botesEnLadoOponente >= 2)
        {
            Debug.Log($"[OASIS] Doble bote en lado del oponente → punto al golpeador ({ultimoGolpeador})");
            if (ultimoGolpeador == Lado.Jefe) JefeAnota();
            else                              JugadorAnota();
            return;
        }

        // Caso 2b: primer bote en lado del oponente → rally continua,
        // ahora el oponente tiene que devolverla.
    }

    /// <summary>
    /// Lo llama BallWatchdog cuando la pelota se pierde (fuera del mundo o
    /// quieta demasiado tiempo).
    ///   - 0 botes en lado oponente → el golpeador la mando afuera → punto al oponente.
    ///   - 1+ botes en lado oponente → el oponente no la devolvio → punto al golpeador.
    ///   - Nadie golpeo todavia → fallo el saque → punto contra el que sacaba.
    /// </summary>
    public void PelotaPerdidaPorWatchdog()
    {
        if (!roundActive || gameOver) return;

        if (ultimoGolpeador == Lado.Ninguno)
        {
            bool jefeSacaba = (numeroRonda % 2 != 0);
            Debug.Log($"[OASIS] Saque fallado por {(jefeSacaba ? "JEFE" : "JUGADOR")} → punto al oponente");
            if (jefeSacaba) JugadorAnota(); else JefeAnota();
            return;
        }

        if (botesEnLadoOponente == 0)
        {
            Debug.Log($"[OASIS] {ultimoGolpeador} la mando afuera (no boto en lado del oponente) → punto al oponente");
            if (ultimoGolpeador == Lado.Jefe) JugadorAnota();
            else                              JefeAnota();
        }
        else
        {
            Debug.Log($"[OASIS] Oponente de {ultimoGolpeador} no devolvio → punto al golpeador");
            if (ultimoGolpeador == Lado.Jefe) JefeAnota();
            else                              JugadorAnota();
        }
    }

    // Compatibilidad con scripts viejos que llamaban RegistrarGolpeRaqueta().
    // Por defecto asumimos que fue el jugador.
    public void RegistrarGolpeRaqueta() => RegistrarGolpe(false);
}