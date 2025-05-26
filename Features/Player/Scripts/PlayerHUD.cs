using Godot;

public sealed partial class PlayerHUD : CanvasLayer
{
    [Export] public Label LeftTeamScore { get; private set; }
    [Export] public Label RightTeamScore { get; private set; }
    [Export] public Label StartCountdown { get; private set; }

    public void UpdateCountdown(int newTime)
    {
        StartCountdown.Text = newTime.ToString();
    }

    public void UpdateScores(int leftTeamScore, int rightTeamScore)
    {
        LeftTeamScore.Text = leftTeamScore.ToString();
        RightTeamScore.Text = rightTeamScore.ToString();
    }
}