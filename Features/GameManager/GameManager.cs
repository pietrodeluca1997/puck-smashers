using System;
using Godot;
using Godot.Collections;

public sealed partial class GameManager : Node3D
{
    public static GameManager Instance { get; private set; }

    [Export] public RigidBody3D Ball { get; private set; }
    
    [Export] public PackedScene PlayerScene { get; private set; }
    [Export] public PackedScene PlayerHUDScene { get; private set; }

    [Export] public Node3D HostSpawnPoint { get; private set; }
    [Export] public Node3D ClientSpawnPoint { get; private set; }

    [Export] public int StartCountdownSeconds { get; private set; }

    [Export] public Camera3D Camera { get; private set; }

    [Export] public Texture2D MouseCursor { get; private set; }
    [Export] public AudioStreamPlayer3D PostHitAudioPlayer { get; private set; }
    [Export] public AudioStreamPlayer3D BallOnPlayerHitAudioPlayer { get; private set; }
    [Export] public AudioStreamPlayer3D PlayerOnHitPlayerAudioPlayer { get; private set; }

    private PlayerHUD playerHUD;

    private Dictionary<int, Timer> playerRespawnTimers = new();

    private Timer startCountdownTimer;
    private int remainingCountdown;
    private bool countdownRunning = false;

    private Vector3 ballStartLocation;

    private int leftTeamScore = 0;
    private int rightTeamScore = 0;
    private int winningScore = 5;

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
        }
        else
        {
            Instance = this;
        }
    }

    public override void _Ready()
    {
        Ball.SetMultiplayerAuthority(1);
        Input.SetCustomMouseCursor(MouseCursor);
    }

    private void ResetRound()
    {
        GD.Print("Resetando a BOLA no " + Multiplayer.IsServer());
        Ball.LinearVelocity = Vector3.Zero;
        Ball.AngularVelocity = Vector3.Zero;
        Ball.GlobalPosition = ballStartLocation;

        foreach(var timer in playerRespawnTimers)
        {
            timer.Value.Stop();
        }

        foreach (Node node in GetTree().GetNodesInGroup("players"))
        {
            if (node is Player player)
            {                
                player.RpcId(player.GetMultiplayerAuthority(), nameof(Player.OnResetRound));
                RespawnPlayer(player);
            }
        }

        Rpc(nameof(ShowCountdownHUD));
        StartCountdown();
    }


    private void StartPlayerRespawn(Player player)
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print("Começando o respawn do player");

        int id = player.PlayerID;

        if (playerRespawnTimers.ContainsKey(id))
        {
            playerRespawnTimers[id].QueueFree();
            playerRespawnTimers.Remove(id);
        }

        Timer timer = new Timer
        {
            WaitTime = 3.0,
            OneShot = true
        };

        timer.Timeout += () => RespawnPlayer(player);
        
        AddChild(timer);

        timer.Start();

        playerRespawnTimers[id] = timer;
    }

    private void RespawnPlayer(Player player)
    {
        GD.Print("Respawnando player");

        Vector3 respawnPosition = player.PlayerID == 1 ? HostSpawnPoint.GlobalPosition : ClientSpawnPoint.GlobalPosition;

        player.Rpc(nameof(Player.Respawn), respawnPosition);

        if (playerRespawnTimers.TryGetValue(player.PlayerID, out var timer))
        {
            timer.QueueFree();
            playerRespawnTimers.Remove(player.PlayerID);
        }
    }

    public void StartCountdown()
    {
        remainingCountdown = StartCountdownSeconds;
        countdownRunning = true;

        if (startCountdownTimer == null)
        {
            startCountdownTimer = new Timer();
            AddChild(startCountdownTimer);
            startCountdownTimer.Timeout += OnStartCountdownTick;
            startCountdownTimer.WaitTime = 1.0f;
        }

        startCountdownTimer.Start();

        Rpc(nameof(UpdateStartCountdown), remainingCountdown);
    }

    private void OnStartCountdownTick()
    {
        remainingCountdown--;
        Rpc(nameof(UpdateStartCountdown), remainingCountdown);

        if (remainingCountdown <= 0)
        {
            countdownRunning = false;
            startCountdownTimer.Stop();
            Rpc(nameof(OnStartCountdownFinished));
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void UpdateStartCountdown(int currentTime)
    {
        remainingCountdown = currentTime;
        playerHUD.UpdateCountdown(currentTime);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void OnStartCountdownFinished()
    {
        playerHUD.StartCountdown.Visible = false;

        if (Multiplayer.IsServer())
        {
            StartGame();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ShowCountdownHUD()
    {
        playerHUD.StartCountdown.Visible = true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void PrepareGame()
    {
        GD.Print("Preparing Game...");

        ballStartLocation = Ball.GlobalPosition;

        Rpc(nameof(SpawnHUD));

        StartCountdown();
    }

    public void StartGame()
    {
        GD.Print("Starting Game...");

        foreach (Node node in GetTree().GetNodesInGroup("players"))
        {
            if (node is Player player)
            {
                player.RpcId(player.GetMultiplayerAuthority(), nameof(Player.StartGame));
            }
        }
    }

    public void PlayPlayerCollisionSound(Node other)
    {
        if (other.GetParent() is Player player)
        {
            PlayerOnHitPlayerAudioPlayer.Play();
        }
        else if (other.Name == "Ball")
        {
            BallOnPlayerHitAudioPlayer.Play();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public async void SpawnPlayer(int playerId, Color color, bool isHost)
    {
        Player player = PlayerScene.Instantiate<Player>();
        player.PlayerID = playerId;
        player.Name = $"Player_{playerId}";

        player.PlayerRigidBody.BodyEntered += PlayPlayerCollisionSound;

        Node3D spawnPoint = isHost ? HostSpawnPoint : ClientSpawnPoint;
        spawnPoint.AddChild(player);

        await ToSignal(GetTree(), "process_frame");

        player.SetNetworkOwner(playerId);
        player.SetPlayerColor(color);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SpawnHUD()
    {
        playerHUD = PlayerHUDScene.Instantiate<PlayerHUD>();
        GetTree().Root.AddChild(playerHUD);
    }

    public Godot.Collections.Dictionary GetMouse3DInfo(Vector2 mousePosition, uint mask)
    {
        Vector3 from = Camera.ProjectRayOrigin(mousePosition);
        Vector3 to = from + Camera.ProjectRayNormal(mousePosition) * 1000f;

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

        PhysicsRayQueryParameters3D query = new()
        {
            From = from,
            To = to,
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = mask
        };

        return spaceState.IntersectRay(query);
    }

    public Vector2 GetMouse2DDirection(Vector3 referencePosition)
    {
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 playerWorldPos = referencePosition;
        Vector2 playerOnScreen = Camera.UnprojectPosition(playerWorldPos);

        return (mouse - playerOnScreen).Normalized();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void UpdateScores(int newLeftTeamScore, int newRightTeamScore)
    {
        leftTeamScore = newLeftTeamScore;
        rightTeamScore = newRightTeamScore;

        playerHUD.UpdateScores(newLeftTeamScore, newRightTeamScore);
    }

    public void OnLeftGoal_Body_Entered(Node3D body)
    {
        if (!Multiplayer.IsServer()) return;

        switch(body.Name)
        {
            case "Ball":
                rightTeamScore++;

                Rpc(nameof(UpdateScores), leftTeamScore, rightTeamScore);
                
                ResetRound();
                

                break;
            case "Player":

                if (body.GetParent() is Player player)
                {
                    GD.Print(body.Name);
                    StartPlayerRespawn(player);
                }
                break;
        }
    }

    public void OnRightGoal_Body_Entered(Node3D body)
    {
        if (!Multiplayer.IsServer()) return;

        switch (body.Name)
        {
            case "Ball":
                leftTeamScore++;

                Rpc(nameof(UpdateScores), leftTeamScore, rightTeamScore);

                ResetRound();
                break;
            case "Player":
                if (body.GetParent() is Player player)
                {
                    GD.Print(body.Name);
                    StartPlayerRespawn(player);
                }
                break;
        }
    }

    public void OnPostHit(Node3D body)
    {
        PostHitAudioPlayer.Play();
    }
}