using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum RallySide
    {
        None,
        Player,
        Boss
    }

    [Header("Lives")]
    [SerializeField] private int bossStartingLife = 15;
    [SerializeField] private int playerStartingLife = 11;

    [Header("UI")]
    public TextMeshProUGUI countdownText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Slider bossLifeSlider;
    public Slider playerLifeSlider;
    public TextMeshProUGUI bossLifeText;
    public TextMeshProUGUI playerLifeText;

    [Header("Scene References")]
    public BallSpawner ballSpawner;

    [Header("Flow")]
    [SerializeField] private float secondsBetweenPoints = 1.75f;
    [SerializeField] private float countdownStepSeconds = 0.85f;
    [SerializeField] private float serveAnnouncementSeconds = 0.5f;

    public bool roundActive { get; private set; }
    public bool gameOver { get; private set; }
    public int bossLife { get; private set; }
    public int playerLife { get; private set; }
    public int bossMaxLife => _bossMaxLife;
    public int playerMaxLife => _playerMaxLife;
    public bool waitingForPlayerServeHit => _waitingForPlayerServeHit;
    public RallySide lastHitter => _lastHitter;

    private int _roundNumber;
    private int _bossMaxLife;
    private int _playerMaxLife;
    private int _serveBounceCount;
    private bool _waitingForPlayerServeHit;
    private bool _serveInProgress;
    private RallySide _serverThisRound = RallySide.Player;
    private RallySide _lastHitter = RallySide.None;
    private RallySide _expectedBounceSide = RallySide.None;
    private RallySide _lastBounceSide = RallySide.None;
    private Coroutine _roundRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _bossMaxLife = Mathf.Max(1, bossStartingLife);
        _playerMaxLife = Mathf.Max(1, playerStartingLife);
        bossLife = _bossMaxLife;
        playerLife = _playerMaxLife;
    }

    private void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        EnsureRuntimeGameplaySetup();
        UpdateLifeUi();

        _roundNumber = 0;
        _roundRoutine = StartCoroutine(BeginRoundRoutine());
    }

    private void EnsureRuntimeGameplaySetup()
    {
        if (ballSpawner == null)
        {
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        }

        if (ballSpawner == null)
        {
            Debug.LogError("[OASIS] No BallSpawner was found in the scene.");
        }

        GameObject paddle = GameObject.Find("Raqueta_Jugador");
        if (paddle != null && paddle.GetComponent<PlayerPaddleController>() == null)
        {
            paddle.AddComponent<PlayerPaddleController>();
        }
    }

    private void UpdateLifeUi()
    {
        if (bossLifeSlider != null)
        {
            bossLifeSlider.minValue = 0f;
            bossLifeSlider.maxValue = _bossMaxLife;
            bossLifeSlider.value = bossLife;
        }

        if (playerLifeSlider != null)
        {
            playerLifeSlider.minValue = 0f;
            playerLifeSlider.maxValue = _playerMaxLife;
            playerLifeSlider.value = playerLife;
        }

        if (bossLifeText != null)
        {
            bossLifeText.text = $"Jefe {BuildLifeText(bossLife, _bossMaxLife)}";
        }

        if (playerLifeText != null)
        {
            playerLifeText.text = $"Tu   {BuildLifeText(playerLife, _playerMaxLife)}";
        }
    }

    private static string BuildLifeText(int current, int max)
    {
        return $"{Mathf.Clamp(current, 0, max)}/{max}";
    }

    public void RegisterPlayerPaddleHit()
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        if (_waitingForPlayerServeHit)
        {
            _waitingForPlayerServeHit = false;
            _lastHitter = RallySide.Player;
            _expectedBounceSide = RallySide.Player;
            _lastBounceSide = RallySide.None;
            BallWatchdog.instance?.RegistrarGolpe();

            if (BallWatchdog.instance != null && !BallWatchdog.instance.IsMonitoring)
            {
                StartCoroutine(ArmWatchdogNextFrame());
            }

            return;
        }

        if (!CanPlayerStrikeBall())
        {
            ReportIllegalStrike(RallySide.Player, "Golpeaste antes de que la pelota botara en tu lado.");
            return;
        }

        _waitingForPlayerServeHit = false;
        _serveInProgress = false;
        _lastHitter = RallySide.Player;
        _expectedBounceSide = RallySide.Boss;
        _lastBounceSide = RallySide.None;
        BallWatchdog.instance?.RegistrarGolpe();

        if (BallWatchdog.instance != null && !BallWatchdog.instance.IsMonitoring)
        {
            StartCoroutine(ArmWatchdogNextFrame());
        }
    }

    public void RegisterBossShotReleased()
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        _waitingForPlayerServeHit = false;
        _lastHitter = RallySide.Boss;
        _expectedBounceSide = _serveInProgress && _serverThisRound == RallySide.Boss && _serveBounceCount == 0
            ? RallySide.Boss
            : RallySide.Player;
        _lastBounceSide = RallySide.None;
        BallWatchdog.instance?.RegistrarGolpe();

        if (BallWatchdog.instance != null && !BallWatchdog.instance.IsMonitoring)
        {
            StartCoroutine(ArmWatchdogNextFrame());
        }
    }

    public void HandleBallPassedZone(SensorPuntos.Zona zona)
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        if (_waitingForPlayerServeHit)
        {
            RestartPlayerServeWithoutScore("La pelota se perdio antes del saque.");
            return;
        }

        RallySide crossedSide = ToSide(zona);
        RallySide scorer = _expectedBounceSide != RallySide.None && _lastBounceSide == _expectedBounceSide
            ? Opposite(crossedSide)
            : ResolveFaultScorerBeforeExpectedBounce();

        AwardPoint(scorer, $"La pelota salio por el lado {crossedSide}.");
    }

    public void ReportBallLost(Vector3 lastKnownPosition)
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        if (_waitingForPlayerServeHit)
        {
            RestartPlayerServeWithoutScore($"Pelota perdida antes del saque en {lastKnownPosition}.");
            return;
        }

        RallySide scorer = _expectedBounceSide != RallySide.None && _lastBounceSide == _expectedBounceSide
            ? Opposite(_expectedBounceSide)
            : ResolveFaultScorerBeforeExpectedBounce();

        AwardPoint(scorer, $"Pelota perdida en {lastKnownPosition}.");
    }

    public void RegisterRebote(bool ladoJefe)
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        if (_waitingForPlayerServeHit)
        {
            return;
        }

        RallySide bounceSide = ladoJefe ? RallySide.Boss : RallySide.Player;

        if (_expectedBounceSide == RallySide.None)
        {
            _expectedBounceSide = bounceSide;
        }

        if (bounceSide != _expectedBounceSide)
        {
            RallySide scorer = ResolveFaultScorerBeforeExpectedBounce();
            AwardPoint(scorer, $"Rebote invalido en lado {(ladoJefe ? "jefe" : "jugador")}.");
            return;
        }

        if (_lastBounceSide == bounceSide)
        {
            AwardPoint(_lastHitter, $"Doble bote en lado {(ladoJefe ? "jefe" : "jugador")}.");
            return;
        }

        _lastBounceSide = bounceSide;

        if (_serveInProgress)
        {
            if (_serveBounceCount == 0)
            {
                _serveBounceCount = 1;
                _expectedBounceSide = Opposite(_serverThisRound);
            }
            else if (_serveBounceCount == 1)
            {
                _serveBounceCount = 2;
                _serveInProgress = false;
            }
        }

        BallWatchdog.instance?.RegistrarGolpe();
    }

    public bool CanPlayerStrikeBall()
    {
        if (gameOver || !roundActive)
        {
            return false;
        }

        if (_waitingForPlayerServeHit)
        {
            return _serverThisRound == RallySide.Player;
        }

        return _lastHitter == RallySide.Boss
            && _expectedBounceSide == RallySide.Player
            && _lastBounceSide == RallySide.Player;
    }

    public bool CanBossStrikeBall()
    {
        if (gameOver || !roundActive || _waitingForPlayerServeHit)
        {
            return false;
        }

        return _lastHitter == RallySide.Player
            && _expectedBounceSide == RallySide.Boss
            && _lastBounceSide == RallySide.Boss;
    }

    public void ReportIllegalStrike(RallySide striker, string reason)
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        AwardPoint(Opposite(striker), reason);
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RestartPlayerServeWithoutScore(string reason)
    {
        Debug.Log($"[OASIS] Reiniciando saque del jugador sin punto. Motivo: {reason}");

        roundActive = false;
        BallWatchdog.instance?.DetenerMonitoreo();
        ballSpawner?.DestruirPelotaActual();

        if (_roundRoutine != null)
        {
            StopCoroutine(_roundRoutine);
        }

        _roundRoutine = StartCoroutine(RestartSameRoundRoutine());
    }

    private IEnumerator RestartSameRoundRoutine()
    {
        yield return new WaitForSeconds(0.65f);
        yield return BeginRoundRoutine();
    }

    private void AwardPoint(RallySide scorer, string reason)
    {
        if (gameOver || !roundActive)
        {
            return;
        }

        roundActive = false;
        BallWatchdog.instance?.DetenerMonitoreo();
        ballSpawner?.DestruirPelotaActual();

        if (scorer == RallySide.Player)
        {
            bossLife = Mathf.Max(0, bossLife - 1);
            Debug.Log($"[OASIS] Punto del jugador. {reason}");
        }
        else
        {
            playerLife = Mathf.Max(0, playerLife - 1);
            Debug.Log($"[OASIS] Punto del jefe. {reason}");
        }

        UpdateLifeUi();

        if (CheckGameOver())
        {
            return;
        }

        _roundNumber++;

        if (_roundRoutine != null)
        {
            StopCoroutine(_roundRoutine);
        }

        _roundRoutine = StartCoroutine(DelayAndStartNextRound());
    }

    private IEnumerator DelayAndStartNextRound()
    {
        yield return new WaitForSeconds(secondsBetweenPoints);
        yield return BeginRoundRoutine();
    }

    private IEnumerator BeginRoundRoutine()
    {
        bool bossServes = (_roundNumber % 2) != 0;
        ResetRallyState(bossServes);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            for (int i = 3; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(countdownStepSeconds);
            }

            countdownText.text = bossServes ? "JEFE!" : "TU!";
            yield return new WaitForSeconds(serveAnnouncementSeconds);
            countdownText.gameObject.SetActive(false);
        }

        roundActive = true;

        if (ballSpawner == null)
        {
            Debug.LogError("[OASIS] BallSpawner is missing.");
            yield break;
        }

        ballSpawner.SpawnBall(bossServes);
    }

    private void ResetRallyState(bool bossServes)
    {
        _serverThisRound = bossServes ? RallySide.Boss : RallySide.Player;
        _serveInProgress = true;
        _serveBounceCount = 0;
        _waitingForPlayerServeHit = !bossServes;
        _lastHitter = RallySide.None;
        _expectedBounceSide = RallySide.None;
        _lastBounceSide = RallySide.None;
        BallWatchdog.instance?.DetenerMonitoreo();
    }

    private IEnumerator ArmWatchdogNextFrame()
    {
        yield return null;
        if (roundActive && !_waitingForPlayerServeHit)
        {
            BallWatchdog.instance?.IniciarMonitoreo();
        }
    }

    private bool CheckGameOver()
    {
        if (bossLife <= 0)
        {
            FinishGame("VICTORIA!\nDerrotaste al jefe.");
            return true;
        }

        if (playerLife <= 0)
        {
            FinishGame("DERROTA\nEl jefe te vencio.");
            return true;
        }

        return false;
    }

    private void FinishGame(string message)
    {
        gameOver = true;
        roundActive = false;
        BallWatchdog.instance?.DetenerMonitoreo();

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = message;
        }

        Time.timeScale = 0f;
    }

    private static RallySide ToSide(SensorPuntos.Zona zona)
    {
        return zona == SensorPuntos.Zona.Jugador ? RallySide.Player : RallySide.Boss;
    }

    private static RallySide Opposite(RallySide side)
    {
        return side switch
        {
            RallySide.Player => RallySide.Boss,
            RallySide.Boss => RallySide.Player,
            _ => RallySide.None
        };
    }

    private RallySide ResolveFaultScorerBeforeExpectedBounce()
    {
        if (_serveInProgress && _serveBounceCount == 0)
        {
            return Opposite(_serverThisRound);
        }

        if (_expectedBounceSide != RallySide.None)
        {
            return _expectedBounceSide;
        }

        RallySide reference = _lastHitter != RallySide.None ? _lastHitter : _serverThisRound;
        return Opposite(reference);
    }
}
