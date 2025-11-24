using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public class Leaderboard : MonoBehaviour
{
    public async void SendScore(string leaderboardId, double score)
    {
        try
        {
            AddPlayerScoreOptions options = new AddPlayerScoreOptions();


            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);

            Debug.Log($"Score submitted successfully! Player ID: {entry.PlayerId} ({entry.PlayerName}), Score: {entry.Score}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to submit score: " + e.Message);
        }
    }

    public async void GetScore(string leaderboardId)
    {
        try
        {
            GetPlayerScoreOptions options = new GetPlayerScoreOptions();
            options.IncludeMetadata = true; // Optional: include metadata if needed

            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, options);
            Debug.Log($"Retrieved score: Player ID: {entry.PlayerId} ({entry.PlayerName}), Score: {entry.Score}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to retrieve score: " + e.Message);
        }
    }

    public async void GetTopScores(string leaderboardId)
    {
        try
        {
            GetScoresOptions options = new GetScoresOptions();
            options.Limit = 10; // Retrieve top 10 scores
            options.IncludeMetadata = true; // Optional: include metadata if needed

            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);

            foreach (LeaderboardEntry entry in page.Results)
            {
                Debug.Log($"Player ID: {entry.PlayerId} ({entry.PlayerName}), Score: {entry.Score}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to retrieve top scores: " + e.Message);
        }
    }

    public async void GetRangeScores(string leaderboardId)
    {
        try
        {
            GetPlayerRangeOptions options = new GetPlayerRangeOptions();
            options.RangeLimit = 2; // Retrieve 2 scores above and below the player

            LeaderboardScores page = await LeaderboardsService.Instance.GetPlayerRangeAsync(leaderboardId, options);
            foreach (LeaderboardEntry entry in page.Results)
            {
                Debug.Log($"Player ID: {entry.PlayerId} ({entry.PlayerName}), Score: {entry.Score}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to retrieve range scores: " + e.Message);
        }
    }
}