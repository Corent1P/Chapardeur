using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using System;
using Sirenix.OdinInspector;
using UnityEditor.XR;

public class Leaderboards : MonoBehaviour
{
    [Button]
    public async void SendScore(string leaderboardId, double score)
    {
        try
        {
            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            print("Player Id: " + entry.PlayerId + " Name: " + entry.PlayerName + " Score: " + entry.Score);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async void SendRandomScore(string leaderboardId)
    {
        try
        {
            double score = UnityEngine.Random.Range(0, 20);
            LeaderboardEntry entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            print("Player Id: " + entry.PlayerId + " Name: " + entry.PlayerName + " Score: " + entry.Score);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [Button]
    public async void GetScore(string leaderboardId)
    {
        try
        {
            GetPlayerScoreOptions opts = new GetPlayerScoreOptions();
            opts.IncludeMetadata = true;

            LeaderboardEntry entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId, opts);

            print("Player Id: " + entry.PlayerId + " Name: " + entry.PlayerName + " Score: " + entry.Score + " Metadata: " + entry.Metadata);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [Button]
    public async void GetTopScores(string leaderboardId)
    {
        GetScoresOptions opts = new GetScoresOptions();
        opts.Limit = 2;

        LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, opts);

        foreach (LeaderboardEntry entry in page.Results)
        {
            print("Player Id: " + entry.PlayerId + " Name: " + entry.PlayerName + " Score: " + entry.Score);
        }
    }

    [Button]
    public async void GetRangeScores(string leaderboardId)
    {
        GetPlayerRangeOptions opts = new GetPlayerRangeOptions();
        opts.RangeLimit = 2;

        LeaderboardScores page = await LeaderboardsService.Instance.GetPlayerRangeAsync(leaderboardId, opts);

        foreach (LeaderboardEntry entry in page.Results)
        {
            print("Player Id: " + entry.PlayerId + " Name: " + entry.PlayerName + " Score: " + entry.Score);
        }
    }
}
