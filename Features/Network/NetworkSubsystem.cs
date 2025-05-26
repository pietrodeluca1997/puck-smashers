using Godot;
using System;
using System.Linq;

public partial class NetworkSubsystem : Node
{
    private ENetMultiplayerPeer multiplayerPeer = new();
    private bool isHost = false;

    private const string HOST_IP = "25.48.187.9";
    private const int HOST_PORT = 7777;

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineArgs();

        if (args.Contains("--host"))
        {
            HostGame();
        }
        else
        {
            JoinGame(HOST_IP);
        }
    }

    private void HostGame()
    {
        multiplayerPeer.CreateServer(HOST_PORT);
        Multiplayer.MultiplayerPeer = multiplayerPeer;
        Multiplayer.PeerConnected += OnPeerConnected;

        isHost = true;

        GD.Print($"Hosting game on port {HOST_PORT}...");
    }

    private void JoinGame(string address)
    {
        multiplayerPeer.CreateClient(address, HOST_PORT);
        Multiplayer.MultiplayerPeer = multiplayerPeer;

        isHost = false;

        GD.Print($"Joining game at {address}:{HOST_PORT}...");
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Player connected: {id}");

        if (Multiplayer.GetPeers().Length == 1)
        {
            GameManager.Instance.Rpc(nameof(GameManager.SpawnPlayer), 1, Colors.Blue, true);
            GameManager.Instance.Rpc(nameof(GameManager.SpawnPlayer), (int)id, Colors.Red, false);
                       
            GameManager.Instance.Rpc(nameof(GameManager.PrepareGame));            
        }
    }
}
