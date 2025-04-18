using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VolleyballTeam
{
    Blue = 0,
    Purple = 1,
    Default = 2
}

public enum Event
{
    HitPurpleGoal = 0,
    HitBlueGoal = 1,
    HitOutOfBounds = 2,
    HitIntoBlueArea = 3,
    HitIntoPurpleArea = 4,
    HitBlueAgent = 5,
    HitPurpleAgent = 6,
}

public class VolleyballEnvController : MonoBehaviour
{
    int ballSpawnSide;

    VolleyballSettings volleyballSettings;

    public VolleyballAgent blueAgent;
    public VolleyballAgent purpleAgent;

    public List<VolleyballAgent> AgentsList = new List<VolleyballAgent>();
    List<Renderer> RenderersList = new List<Renderer>();

    Rigidbody blueAgentRb;
    Rigidbody purpleAgentRb;

    public GameObject ball;
    Rigidbody ballRb;

    public GameObject blueGoal;
    public GameObject purpleGoal;

    Renderer blueGoalRenderer;

    Renderer purpleGoalRenderer;

    VolleyballTeam lastHitter;

    private int resetTimer;
    public int MaxEnvironmentSteps;
    public int blueScore;
    public int purpleScore;
    public string prevGame;

    void Start()
    {

        // Used to control agent & ball starting positions
        blueAgentRb = blueAgent.GetComponent<Rigidbody>();
        purpleAgentRb = purpleAgent.GetComponent<Rigidbody>();
        ballRb = ball.GetComponent<Rigidbody>();

        // Starting ball spawn side
        // -1 = spawn blue side, 1 = spawn purple side
        var spawnSideList = new List<int> { -1, 1 };
        ballSpawnSide = spawnSideList[Random.Range(0, 2)];

        // Render ground to visualise which agent scored
        blueGoalRenderer = blueGoal.GetComponent<Renderer>();
        purpleGoalRenderer = purpleGoal.GetComponent<Renderer>();
        RenderersList.Add(blueGoalRenderer);
        RenderersList.Add(purpleGoalRenderer);

        volleyballSettings = FindObjectOfType<VolleyballSettings>();

        blueScore = 0;
        purpleScore = 0;

        ResetScene();
    }

    /// <summary>
    /// Tracks which agent last had control of the ball
    /// </summary>
    public void UpdateLastHitter(VolleyballTeam team)
    {
        lastHitter = team;
    }

    /// <summary>
    /// Resolves scenarios when ball enters a trigger and assigns rewards
    /// </summary>
    public void ResolveEvent(Event triggerEvent)
    {

        if (blueScore >= 21 || purpleScore >= 21)
        {
            if (Mathf.Abs(blueScore - purpleScore) >= 2 || blueScore >= 30 || purpleScore >= 30) // Not deuce or 30 points
            {
                prevGame = "Blue: " + blueScore + " - Purple: " + purpleScore;
                blueScore = 0;
                purpleScore = 0;
            }
        }

        switch (triggerEvent)
        {
            case Event.HitOutOfBounds:
                if (lastHitter == VolleyballTeam.Blue)
                {
                    // apply penalty to blue agent
                    blueAgent.AddReward(-0.7f);
                    StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.purpleGoalMaterial, RenderersList, .5f));
                    purpleScore += 1;
                }
                else if (lastHitter == VolleyballTeam.Purple)
                {
                    // apply penalty to purple agent
                    purpleAgent.AddReward(-0.7f);
                    StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));
                    blueScore += 1;
                }

                // end episode
                blueAgent.EndEpisode();
                purpleAgent.EndEpisode();
                ResetScene();
                break;

            case Event.HitBlueGoal:
                // blue wins
                purpleAgent.AddReward(-1.0f);
                blueAgent.AddReward(1.5f);
                // turn floor blue
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.blueGoalMaterial, RenderersList, .5f));
                blueScore += 1;

                // end episode
                blueAgent.EndEpisode();
                purpleAgent.EndEpisode();
                ResetScene();
                break;

            case Event.HitPurpleGoal:
                // purple wins
                blueAgent.AddReward(-1.0f);
                purpleAgent.AddReward(1.5f);
                // turn floor purple
                StartCoroutine(GoalScoredSwapGroundMaterial(volleyballSettings.purpleGoalMaterial, RenderersList, .5f));
                purpleScore += 1;

                // end episode
                blueAgent.EndEpisode();
                purpleAgent.EndEpisode();
                ResetScene();
                break;

            case Event.HitIntoBlueArea:
                if (lastHitter == VolleyballTeam.Purple)
                {
                    purpleAgent.AddReward(0.7f);
                }
                break;

            case Event.HitIntoPurpleArea:
                if (lastHitter == VolleyballTeam.Blue)
                {
                    blueAgent.AddReward(0.7f);
                }
                break;

            case Event.HitBlueAgent:
                if (lastHitter == VolleyballTeam.Purple)
                {
                    // blueAgent.AddReward(0.3f);
                }
                break;

            case Event.HitPurpleAgent:
                if (lastHitter == VolleyballTeam.Blue)
                {
                    // purpleAgent.AddReward(0.3f);
                }
                break;
        }
    }

    /// <summary>
    /// Changes the color of the ground for a moment.
    /// </summary>
    /// <returns>The Enumerator to be used in a Coroutine.</returns>
    /// <param name="mat">The material to be swapped.</param>
    /// <param name="time">The time the material will remain.</param>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, List<Renderer> rendererList, float time)
    {
        foreach (var renderer in rendererList)
        {
            renderer.material = mat;
        }

        yield return new WaitForSeconds(time); // wait for 2 sec

        foreach (var renderer in rendererList)
        {
            renderer.material = volleyballSettings.defaultMaterial;
        }

    }

    /// <summary>
    /// Called every step. Control max env steps.
    /// </summary>
    void FixedUpdate()
    {
        resetTimer += 1;
        if (resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            blueAgent.EpisodeInterrupted();
            purpleAgent.EpisodeInterrupted();
            ResetScene();
        }
    }

    /// <summary>
    /// Reset agent and ball spawn conditions.
    /// </summary>
    public void ResetScene()
    {
        resetTimer = 0;

        lastHitter = VolleyballTeam.Default;

        foreach (var agent in AgentsList)
        {
            var randomPosX = Random.Range(-2f, 2f);
            var randomPosZ = Random.Range(-2f, 2f);
            var randomPosY = Random.Range(0.5f, 3.75f); // Change depending on jump height
            var randomRot = Random.Range(-45f, 45f); // Random rotation

            agent.transform.localPosition = new Vector3(randomPosX, randomPosY, randomPosZ);
            agent.transform.eulerAngles = new Vector3(0, randomRot, 0);

            agent.GetComponent<Rigidbody>().velocity = default(Vector3);
        }

        ResetBall();
    }

    /// <summary>
    /// Reset ball spawn conditions
    /// </summary>
    void ResetBall()
    {
        var randomPosX = Random.Range(-2f, 2f);
        var randomPosY = Random.Range(6f, 10f);
        var randomPosZ = Random.Range(6f, 8f);

        ballSpawnSide = -1 * ballSpawnSide;

        if (ballSpawnSide == -1)
        {
            ball.transform.localPosition = new Vector3(randomPosX, randomPosY, randomPosZ);
        }
        else if (ballSpawnSide == 1)
        {
            ball.transform.localPosition = new Vector3(randomPosX, randomPosY, -1 * randomPosZ);
        }

        ballRb.angularVelocity = Vector3.zero;
        ballRb.velocity = Vector3.zero;
    }
}
