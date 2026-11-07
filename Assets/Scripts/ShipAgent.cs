using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

public class ShipAgent : Agent
{
    private const int ObservedObstacleCount = 3;

    public static ShipAgent Instance { get; private set; }
    public static event Action ModelInferenceAgentDied;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private BehaviorParameters behaviorParameters;
    private Vector2 moveDirection;
    private float decisionTimer;
    private readonly Transform[] nearestObstacles = new Transform[ObservedObstacleCount];
    private readonly float[] nearestObstacleDistances = new float[ObservedObstacleCount];
    private string recordingSessionId;
    private string recordingSessionStartedAt;
    private string episodeEndReason;
    private int episodeNumber;
    private int sessionEpisodeCount;
    private int episodeAsteroidsDestroyed;
    private int sessionAsteroidsDestroyed;
    private float episodeSurvivalSeconds;
    private float sessionSurvivalSeconds;
    private bool isRecordingSession;
    private bool isRecordingEpisode;
    private bool reportWriteFailed;

    [SerializeField] private float actionInterval = 0.2f;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float xBound = 3.33f;
    [SerializeField] private float yBound = 5.5f;
    [SerializeField] private int maxEpisodeSteps = 3000;
    [SerializeField] private float timePenaltyPerSecond = 0.001f;
    [SerializeField] private float asteroidDestroyedReward = 1f;
    [SerializeField] private float asteroidHitReward = 0.05f;
    [SerializeField] private float missedShotPenalty = 0.01f;

    public bool IsControllingShip => isActiveAndEnabled;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        behaviorParameters = GetComponent<BehaviorParameters>();
        Instance = this;
        MaxStep = IsUsingModelInference() ? 0 : maxEpisodeSteps;
    }

    public override void OnEpisodeBegin()
    {
        bool isModelInference = IsUsingModelInference();
        MaxStep = isModelInference ? 0 : maxEpisodeSteps;

        if (isRecordingEpisode)
        {
            WriteEpisodeRecord(episodeEndReason);
        }

        rb.position = new Vector2(0f, -3f);
        rb.linearVelocity = Vector2.zero;
        moveDirection = Vector2.zero;
        decisionTimer = 0f;

        if (playerMovement != null)
        {
            playerMovement.ResetForAgentEpisode();
        }

        if (isModelInference)
        {
            BeginRecordingEpisode();
        }
        else
        {
            isRecordingEpisode = false;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 shipPosition = rb.position;
        sensor.AddObservation(shipPosition.x / xBound);
        sensor.AddObservation(shipPosition.y / yBound);

        for (int i = 0; i < ObservedObstacleCount; i++)
        {
            nearestObstacles[i] = null;
            nearestObstacleDistances[i] = float.MaxValue;
        }

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            float distance = Vector2.Distance(shipPosition, obstacle.transform.position);
            for (int i = 0; i < ObservedObstacleCount; i++)
            {
                if (distance >= nearestObstacleDistances[i])
                {
                    continue;
                }

                for (int j = ObservedObstacleCount - 1; j > i; j--)
                {
                    nearestObstacleDistances[j] = nearestObstacleDistances[j - 1];
                    nearestObstacles[j] = nearestObstacles[j - 1];
                }

                nearestObstacleDistances[i] = distance;
                nearestObstacles[i] = obstacle.transform;
                break;
            }
        }

        for (int i = 0; i < ObservedObstacleCount; i++)
        {
            if (nearestObstacles[i] == null)
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                continue;
            }

            Vector2 relativePosition = (Vector2)nearestObstacles[i].position - shipPosition;
            sensor.AddObservation(relativePosition.x / (2f * xBound));
            sensor.AddObservation(relativePosition.y / (2f * yBound));
        }

        float healthRatio = playerMovement != null ? playerMovement.HealthRatio : 1f;
        sensor.AddObservation(healthRatio);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int xAction = actions.DiscreteActions[0];
        int yAction = actions.DiscreteActions[1];
        int shootAction = actions.DiscreteActions[2];

        moveDirection = new Vector2(
            xAction == 1 ? -1f : xAction == 2 ? 1f : 0f,
            yAction == 1 ? 1f : yAction == 2 ? -1f : 0f).normalized;

        if (shootAction == 1 && PhaserWeapon.Instance != null)
        {
            PhaserWeapon.Instance.Shoot();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> actions = actionsOut.DiscreteActions;
        actions[0] = 0;
        actions[1] = 0;
        actions[2] = 1;

        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        Transform closestObstacle = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject obstacle in obstacles)
        {
            float distance = Vector2.Distance(rb.position, obstacle.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObstacle = obstacle.transform;
            }
        }

        if (closestObstacle == null)
        {
            return;
        }

        float relativeX = closestObstacle.position.x - rb.position.x;
        float relativeY = closestObstacle.position.y - rb.position.y;
        if (relativeY > -1.5f && relativeY < 3f && Mathf.Abs(relativeX) < 1.5f)
        {
            actions[0] = relativeX >= 0f ? 1 : 2;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        if (isRecordingEpisode)
        {
            episodeSurvivalSeconds += deltaTime;
            sessionSurvivalSeconds += deltaTime;
        }

        AddReward(-timePenaltyPerSecond * Time.deltaTime);
        decisionTimer += Time.deltaTime;
        if (decisionTimer >= Mathf.Max(actionInterval, Time.deltaTime))
        {
            decisionTimer = 0f;
            RequestDecision();
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    protected override void OnDisable()
    {
        if (Application.isPlaying && Instance == this)
        {
            FinishRecordingSession();
        }

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDisable();
    }

    public void OnPlayerDamaged()
    {
        AddReward(-0.1f);
    }

    public void OnPlayerDestroyed()
    {
        bool stopModelInferenceSession = isRecordingEpisode && IsUsingModelInference();
        if (stopModelInferenceSession)
        {
            episodeEndReason = "destroyed";
        }

        AddReward(-1f);
        if (stopModelInferenceSession)
        {
            ModelInferenceAgentDied?.Invoke();
            return;
        }

        EndEpisode();
    }

    public void OnAsteroidDestroyed()
    {
        if (isRecordingEpisode)
        {
            episodeAsteroidsDestroyed++;
            sessionAsteroidsDestroyed++;
        }

        AddReward(asteroidDestroyedReward);
    }

    public void OnAsteroidHit()
    {
        AddReward(asteroidHitReward);
    }

    public void OnShotMissed()
    {
        AddReward(-missedShotPenalty);
    }

    public static void SavePlaySessionReport()
    {
        if (Instance != null)
        {
            Instance.FinishRecordingSession();
        }
    }

    private bool IsUsingModelInference()
    {
        if (behaviorParameters == null || behaviorParameters.Model == null)
        {
            return false;
        }

        if (behaviorParameters.BehaviorType == BehaviorType.InferenceOnly)
        {
            return true;
        }

        return behaviorParameters.BehaviorType == BehaviorType.Default
            && !Academy.Instance.IsCommunicatorOn;
    }

    private void BeginRecordingEpisode()
    {
        if (!isRecordingSession)
        {
            recordingSessionId = Guid.NewGuid().ToString("N");
            recordingSessionStartedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            episodeNumber = 0;
            sessionEpisodeCount = 0;
            sessionAsteroidsDestroyed = 0;
            sessionSurvivalSeconds = 0f;
            reportWriteFailed = false;
            isRecordingSession = true;
        }

        episodeNumber++;
        sessionEpisodeCount++;
        episodeAsteroidsDestroyed = 0;
        episodeSurvivalSeconds = 0f;
        episodeEndReason = "episode_end";
        isRecordingEpisode = true;
    }

    private void WriteEpisodeRecord(string endReason)
    {
        if (!AppendCsvRecord(string.Join(",",
            "episode",
            EscapeCsv(recordingSessionId),
            EscapeCsv(recordingSessionStartedAt),
            episodeNumber.ToString(CultureInfo.InvariantCulture),
            episodeSurvivalSeconds.ToString("F3", CultureInfo.InvariantCulture),
            episodeAsteroidsDestroyed.ToString(CultureInfo.InvariantCulture),
            EscapeCsv(endReason),
            string.Empty,
            string.Empty,
            string.Empty)))
        {
            reportWriteFailed = true;
        }

        isRecordingEpisode = false;
    }

    private void FinishRecordingSession()
    {
        if (!isRecordingSession)
        {
            return;
        }

        if (isRecordingEpisode)
        {
            WriteEpisodeRecord(episodeEndReason == "destroyed" ? episodeEndReason : "stopped");
        }

        if (!AppendCsvRecord(string.Join(",",
            "session_summary",
            EscapeCsv(recordingSessionId),
            EscapeCsv(recordingSessionStartedAt),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            sessionEpisodeCount.ToString(CultureInfo.InvariantCulture),
            sessionSurvivalSeconds.ToString("F3", CultureInfo.InvariantCulture),
            sessionAsteroidsDestroyed.ToString(CultureInfo.InvariantCulture))))
        {
            reportWriteFailed = true;
        }

        isRecordingSession = false;
        if (reportWriteFailed)
        {
            Debug.LogError($"Agent inference report may be incomplete. Check the Console for write errors. Report path: {GetReportPath()}");
        }
        else
        {
            Debug.Log($"Agent inference report saved to: {GetReportPath()}");
        }
    }

    private static bool AppendCsvRecord(string record)
    {
        string reportPath = GetReportPath();
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            bool needsHeader = !File.Exists(reportPath) || new FileInfo(reportPath).Length == 0;
            using (StreamWriter writer = new StreamWriter(reportPath, true, new UTF8Encoding(true)))
            {
                if (needsHeader)
                {
                    writer.WriteLine("record_type,session_id,started_at_utc,episode_number,survival_seconds,asteroids_destroyed,end_reason,total_episodes,total_survival_seconds,total_asteroids_destroyed");
                }

                writer.WriteLine(record);
            }

            return true;
        }
        catch (IOException exception)
        {
            Debug.LogError($"Could not write agent inference report to '{reportPath}': {exception.Message}");
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            Debug.LogError($"Could not write agent inference report to '{reportPath}': {exception.Message}");
            return false;
        }
    }

    private static string GetReportPath()
    {
        return Path.Combine(Application.persistentDataPath, "AgentPlaySessions.csv");
    }

    private static string EscapeCsv(string value)
    {
        return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }

    private void ApplyMovement()
    {
        Vector2 position = rb.position;
        position.x = Mathf.Clamp(position.x, -xBound, xBound);
        position.y = Mathf.Clamp(position.y, -yBound, yBound);
        rb.position = position;

        Vector2 velocity = moveDirection * speed;
        if ((position.x <= -xBound && velocity.x < 0f) || (position.x >= xBound && velocity.x > 0f))
        {
            velocity.x = 0f;
        }
        if ((position.y <= -yBound && velocity.y < 0f) || (position.y >= yBound && velocity.y > 0f))
        {
            velocity.y = 0f;
        }

        rb.linearVelocity = velocity;
    }
}
