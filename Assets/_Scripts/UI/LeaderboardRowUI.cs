using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject highlightRoot;

    public void SetData(int rank, string username, int score, bool highlight)
    {
        if (rankText != null)
        {
            rankText.text = rank > 0 ? rank.ToString() : "-";
        }

        if (usernameText != null)
        {
            usernameText.text = string.IsNullOrWhiteSpace(username) ? "-" : username;
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (highlightRoot != null)
        {
            highlightRoot.SetActive(highlight);
        }
    }
}

