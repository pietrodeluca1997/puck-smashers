using Godot;
using Godot.Collections;
using static Godot.MultiplayerApi;

public partial class Player : Node3D
{
    [Export]
    public int PlayerID;

    [Export]
    public RigidBody3D PlayerRigidBody {  get; private set; }

    [Export]
    public ProgressBar DirectionAndForceBar { get; private set; }

    [Export]
    public Node3D DirectionAndForceBarMeshPivot { get; private set; }

    [Export]
    public MeshInstance3D MeshInstance { get; private set; }

    private StandardMaterial3D material;

    private bool isHolding;
    private float forceIncreaseFactor = 50.0f;

    private Vector2 lastMouse2DDirection = Vector2.Zero;

    public override void _Ready()
    {
        AddToGroup("players");

        Freeze();

        material = (StandardMaterial3D)MeshInstance.MaterialOverride;

        isHolding = false;

        DirectionAndForceBarMeshPivot.Visible = false;
    }

    [Rpc(RpcMode.AnyPeer, CallLocal = true)]
    public void StartGame()
    {
        Unfreeze();
    }

    [Rpc(RpcMode.AnyPeer, CallLocal = true)]
    public void OnResetRound()
    {
        Freeze();
    }

    public void Freeze()
    {
        PlayerRigidBody.Freeze = true;
        PlayerRigidBody.LinearVelocity = Vector3.Zero;
        PlayerRigidBody.AngularVelocity = Vector3.Zero;        
    }

    public void Unfreeze()
    {
        PlayerRigidBody.Freeze = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.GetUniqueId() != GetMultiplayerAuthority())
            return;

        Vector3 pivotPos = DirectionAndForceBarMeshPivot.GlobalPosition;
        pivotPos.X = PlayerRigidBody.GlobalPosition.X;
        pivotPos.Z = PlayerRigidBody.GlobalPosition.Z;
        DirectionAndForceBarMeshPivot.GlobalPosition = pivotPos;

        if (isHolding)
        {
            DirectionAndForceBar.Value += forceIncreaseFactor * (float)delta;

            Vector2 directionToMouse = GameManager.Instance.GetMouse2DDirection(PlayerRigidBody.GlobalPosition);

            lastMouse2DDirection = directionToMouse;

            float angle = Mathf.Atan2(directionToMouse.X, directionToMouse.Y);

            DirectionAndForceBarMeshPivot.Rotation = new Vector3(0, angle, 0);
        }
        else
        {
            DirectionAndForceBarMeshPivot.Visible = false;

            Vector3 impulse = Vector3.Zero;

            impulse.X = (float)(lastMouse2DDirection.X * -DirectionAndForceBar.Value);
            impulse.Z = (float)(lastMouse2DDirection.Y * -DirectionAndForceBar.Value);

            PlayerRigidBody.ApplyCentralImpulse(impulse);

            DirectionAndForceBar.Value = 0.0f;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Multiplayer.GetUniqueId() != GetMultiplayerAuthority())
            return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {                
                Dictionary result = GameManager.Instance.GetMouse3DInfo(mouseEvent.Position, 2);

                if (!result.ContainsKey("collider")) return;

                Node collider = (Node)result["collider"];

                if (collider.Name == "PlayerInteractArea" && collider.IsMultiplayerAuthority())
                {
                    DirectionAndForceBarMeshPivot.Visible = true;
                    isHolding = true;
                }
            }
            else
            {
                isHolding = false;
            }
        }
    }

    [Rpc(CallLocal = true)]
    public void SetNetworkOwner(int peerId)
    {
        SetMultiplayerAuthority(peerId);
    }

    [Rpc(CallLocal = true)]
    public void SetPlayerColor(Color color)
    {
        material.AlbedoColor = color;
    }

    [Rpc(RpcMode.AnyPeer, CallLocal = true)]
    public void Respawn(Vector3 position)
    {
        PlayerRigidBody.LinearVelocity = Vector3.Zero;
        PlayerRigidBody.AngularVelocity = Vector3.Zero;

        PlayerRigidBody.Sleeping = true;
        PlayerRigidBody.GlobalPosition = position;
        PlayerRigidBody.Sleeping = false;
    }
}
