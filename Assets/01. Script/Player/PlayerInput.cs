using UnityEngine;
public interface IPlayerInput
{
    Vector2 Move { get; }
    bool Sprint { get; }
    bool JumpPressedThisFrame { get; }

    void Poll(); // Update에서 호출
}
public class PlayerInput : MonoBehaviour, IPlayerInput
{
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode jumpKey = KeyCode.Space;

    public Vector2 Move { get; private set; }
    public bool Sprint { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }

    public void Poll()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Move = new Vector2(h, v);
        Sprint = Input.GetKey(sprintKey);
        JumpPressedThisFrame = Input.GetKeyDown(jumpKey);
    }
}
