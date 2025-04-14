using UnityEngine;

public class UserAgent : MonoBehaviour
{
  public GameObject self;
  public VolleyballTeam teamId;
  public GameObject area;
  Rigidbody agentRb;

  // Controls jump behavior
  float jumpingTime;
  Vector3 jumpTargetPos;
  Vector3 jumpStartingPos;
  float agentRot;
  VolleyballSettings volleyballSettings;
  VolleyballEnvController envController;

  public Collider[] hitGroundColliders = new Collider[3];

  void Start()
  {
    envController = area.GetComponent<VolleyballEnvController>();
    volleyballSettings = GameObject.FindObjectOfType<VolleyballSettings>();

    agentRb = self.GetComponent<Rigidbody>();
    agentRot = 1;
  }

  /// <summary>
  /// Moves  a rigidbody towards a position smoothly.
  /// </summary>
  /// <param name="targetPos">Target position.</param>
  /// <param name="rb">The rigidbody to be moved.</param>
  /// <param name="targetVel">The velocity to target during the
  ///  motion.</param>
  /// <param name="maxVel">The maximum velocity posible.</param>
  void MoveTowards(
      Vector3 targetPos, Rigidbody rb, float targetVel, float maxVel)
  {
    var moveToPos = targetPos - rb.worldCenterOfMass;
    var velocityTarget = Time.fixedDeltaTime * targetVel * moveToPos;
    if (float.IsNaN(velocityTarget.x) == false)
    {
      rb.velocity = Vector3.MoveTowards(
          rb.velocity, velocityTarget, maxVel);
    }
  }

  /// <summary>
  /// Check if agent is on the ground to enable/disable jumping
  /// </summary>
  public bool CheckIfGrounded()
  {
    hitGroundColliders = new Collider[3];
    var o = self;
    Physics.OverlapBoxNonAlloc(
        o.transform.localPosition + new Vector3(0, -0.05f, 0),
        new Vector3(0.95f / 2f, 0.5f, 0.95f / 2f),
        hitGroundColliders,
        o.transform.rotation);
    var grounded = false;
    foreach (var col in hitGroundColliders)
    {
      if (col != null && col.transform != self.transform &&
          (col.CompareTag("walkableSurface") ||
           col.CompareTag("purpleGoal") ||
           col.CompareTag("blueGoal")))
      {
        grounded = true; //then we're grounded
        break;
      }
    }
    return grounded;
  }

  /// <summary>
  /// Called when agent collides with the ball
  /// </summary>
  void OnCollisionEnter(Collision c)
  {
    if (c.gameObject.CompareTag("ball"))
    {
      envController.UpdateLastHitter(teamId);
    }
  }

  /// <summary>
  /// Starts the jump sequence
  /// </summary>
  public void Jump()
  {
    jumpingTime = 0.3f;
    jumpStartingPos = agentRb.position;
  }

  /// <summary>
  /// Resolves the agent movement
  /// </summary>
  public void FixedUpdate()
  {
    var dirToGo = Vector3.zero;

    var grounded = CheckIfGrounded();

    if (Input.GetKey(KeyCode.W))
      dirToGo = (grounded ? 1f : 0.5f) * transform.forward * 1f;
    if (Input.GetKey(KeyCode.S))
      dirToGo = (grounded ? 1f : 0.5f) * transform.forward * -1f;

    agentRb.AddForce(agentRot * dirToGo * volleyballSettings.agentRunSpeed,
        ForceMode.VelocityChange);

    dirToGo = Vector3.zero;

    if (Input.GetKey(KeyCode.A))
      dirToGo = (grounded ? 1f : 0.5f) * transform.right * -1f;
    if (Input.GetKey(KeyCode.D))
      dirToGo = (grounded ? 1f : 0.5f) * transform.right * 1f;

    agentRb.AddForce(agentRot * dirToGo * volleyballSettings.agentRunSpeed,
        ForceMode.VelocityChange);

    // makes the agent physically "jump"
    if (jumpingTime > 0f)
    {
      jumpTargetPos =
          new Vector3(agentRb.position.x,
              jumpStartingPos.y + volleyballSettings.agentJumpHeight,
              agentRb.position.z) + agentRot * dirToGo;

      MoveTowards(jumpTargetPos, agentRb, volleyballSettings.agentJumpVelocity,
          volleyballSettings.agentJumpVelocityMaxChange);
    }

    // provides a downward force to end the jump
    if (!(jumpingTime > 0f) && !grounded)
    {
      agentRb.AddForce(
          Vector3.down * volleyballSettings.fallingForce, ForceMode.Acceleration);
    }

    // controls the jump sequence
    if (jumpingTime > 0f)
    {
      jumpingTime -= Time.fixedDeltaTime;
    }

    if (Input.GetKey(KeyCode.Space))
    {
      if (((jumpingTime <= 0f) && grounded))
      {
        Jump();
      }
    }
  }
}
