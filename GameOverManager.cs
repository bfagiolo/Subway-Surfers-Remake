using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverCanvas;
    public GameObject gameOverPanel; // Reference to the Game Over UI panel
    public GameObject highScoreCanvas; // Reference to the High Score Canvas
    public TextMeshProUGUI FinalScoreText; // Reference to the TextMeshProUGUI for the final score in the Game Over panel
    public TextMeshProUGUI HighScoreFinalScoreText; // Reference to the TextMeshProUGUI for the final score in the High Score canvas
    public TextMeshProUGUI FinalCoinsText; // Reference to the TextMeshProUGUI for the final coins
    public ScoreDisplayManager scoreManager; // Reference to the ScoreDisplayManager script
    public CharacterDance characterDance; // Reference to the CharacterDance script
    public Animator slowMoCharacterAnimator; // Reference to the Animator for the separate character
    public TextMeshProUGUI gameOverMessageText; // Reference to the TextMeshProUGUI for the game over message
    public Animator[] shapeAnimators; // Array of animators for shapes
    public AudioClip gameOverSong; // Reference to the AudioClip for the song
    private AudioSource audioSource; // Internal AudioSource to play the song

    private bool isScoreRecorded = false;
    private bool isHighScoreCanvasActive = false;
    private Vector3 initialScale; // Store the initial scale of the HighScoreFinalScoreText

    void Start()
    {
        if (gameOverCanvas == null)
        {
            Debug.LogError("GameOverCanvas is not assigned. Please assign it in the Inspector.");
        }
        else
        {
            gameOverCanvas.SetActive(false);
        }

        if (highScoreCanvas != null)
        {
            highScoreCanvas.SetActive(false);
        }
        else
        {
            Debug.LogWarning("HighScoreCanvas is not assigned.");
        }

        scoreManager = FindObjectOfType<ScoreDisplayManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreDisplayManager is not found in the scene.");
        }

        if (characterDance == null)
        {
            characterDance = FindObjectOfType<CharacterDance>();
            if (characterDance == null)
            {
                Debug.LogError("CharacterDance script is not found in the scene.");
            }
        }

        if (HighScoreFinalScoreText != null)
        {
            initialScale = HighScoreFinalScoreText.transform.localScale;
            HighScoreFinalScoreText.transform.localScale = Vector3.zero; // Start hidden
        }
        else
        {
            Debug.LogWarning("HighScoreFinalScoreText is not assigned.");
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        if (audioSource != null)
        {
            Debug.Log("song time was set");
            audioSource.clip = gameOverSong;
            audioSource.volume = 0f; // Start at 0 volume
            audioSource.time = 25f; // Start 20 seconds into the song
            audioSource.loop = true;
        }
        else
        {
            Debug.LogError("AudioSource or GameOverSong is not assigned.");
        }

        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = ""; // Initially blank
        }
        else
        {
            Debug.LogWarning("GameOverMessageText is not assigned.");
        }
    }

    public void Part2GameOver()
    {
        Debug.Log("The Game Over Function was called");

        if (gameOverCanvas == null)
        {
            Debug.LogError("GameOverCanvas is missing or destroyed. Cannot show Game Over screen.");
            return;
        }

        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager is missing. Cannot calculate final scores.");
            return;
        }

        float finalDistance = Mathf.Abs(scoreManager.playerTransform.position.x - scoreManager.GetStartX());

        if (FinalScoreText != null)
        {
            FinalScoreText.text = finalDistance.ToString("F0");
        }
        else
        {
            Debug.LogError("FinalScoreText is not assigned.");
        }

        if (FinalCoinsText != null)
        {
            FinalCoinsText.text = scoreManager.GetCoinCount().ToString();
        }
        else
        {
            Debug.LogError("FinalCoinsText is not assigned.");
        }

        float greatestDistance = 0f;

        if (ScoreDatabase.Instance != null && ScoreDatabase.Instance.TopScores != null && ScoreDatabase.Instance.TopScores.Count > 0)
        {
            greatestDistance = ScoreDatabase.Instance.TopScores[0].Distance;
        }

        if (!isScoreRecorded) // Record score only once
        {
            EndGame(finalDistance);
            isScoreRecorded = true;
        }

        if (finalDistance > greatestDistance && highScoreCanvas != null)
        {
            Debug.Log("New high score achieved!");
            StartCoroutine(ShowHighScoreCanvas(finalDistance));
        }

        ContinueGameOverSequence();
    }

    private IEnumerator ShowHighScoreCanvas(float finalScore)
    {
        if (highScoreCanvas == null)
        {
            Debug.LogError("HighScoreCanvas is not assigned.");
            yield break;
        }

        isHighScoreCanvasActive = true;
        highScoreCanvas.SetActive(true);

        foreach (var animator in shapeAnimators)
        {
            if (animator != null)
            {
                animator.Play("ShapeAnimation");
            }
            else
            {
                Debug.LogWarning("One of the shapeAnimators is null.");
            }
        }

        if (slowMoCharacterAnimator != null)
        {
            slowMoCharacterAnimator.Play("RunSlowMo");
        }
        else
        {
            Debug.LogError("SlowMoCharacterAnimator is not assigned.");
        }

        if (HighScoreFinalScoreText != null)
        {
            HighScoreFinalScoreText.text = finalScore.ToString("F0");
            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(0f, 1f, elapsed / duration);
                HighScoreFinalScoreText.transform.localScale = initialScale * scale;
                yield return null;
            }

            HighScoreFinalScoreText.transform.localScale = initialScale;
        }

        yield return new WaitForSecondsRealtime(6);
        highScoreCanvas.SetActive(false);
        isHighScoreCanvasActive = false;
    }

    private IEnumerator FadeInMusic()
    {
        audioSource.time = 25f;
        
        Debug.Log("Song is playing.");
        float targetVolume = 1f;
        float duration = 15f;
        float elapsed = 0f;
        audioSource.Play();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator ContinueGameOverSequenceCoroutine()
    {
        if (gameOverCanvas == null)
        {
            Debug.LogError("GameOverCanvas is not assigned.");
            yield break;
        }

        if (audioSource != null && gameOverSong != null)
        {
            StartCoroutine(FadeInMusic());
            Debug.Log("fade in called");// Call FadeInMusic here
        }

        while (isHighScoreCanvasActive)
        {
            yield return null; // Wait for the next frame
        }

        

        gameOverCanvas.SetActive(true);

        if (gameOverMessageText != null)
        {
            StartTypeEffect("Game Over.");
        }

        yield return new WaitForSecondsRealtime(4.5f);

        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = "";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (characterDance != null)
        {
            characterDance.TriggerDance();
        }
        else
        {
            Debug.LogError("CharacterDance script is missing or not assigned.");
        }

        Time.timeScale = 0f;
    }

    private Coroutine typingCoroutine;

    private IEnumerator TypeTextEffect(string message)
    {
        if (gameOverMessageText == null)
        {
            Debug.LogError("GameOverMessageText is not assigned.");
            yield break;
        }

        gameOverMessageText.text = ""; // Clear the text before starting
        foreach (char letter in message.ToCharArray())
        {
            gameOverMessageText.text += letter; // Add one letter at a time
            yield return new WaitForSeconds(0.1f); // Adjust this value for typing speed
        }

        typingCoroutine = null; // Reset coroutine reference when done
    }

    public void StartTypeEffect(string message)
    {
        // If a typing effect is already running, do nothing
        if (typingCoroutine != null)
        {
            Debug.LogWarning("Typing effect is already running. Ignoring additional call.");
            return;
        }

        // Start a new typing coroutine and store its reference
        typingCoroutine = StartCoroutine(TypeTextEffect(message));
    }

    private void ContinueGameOverSequence()
    {
        StartCoroutine(ContinueGameOverSequenceCoroutine());
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private string GenerateFixedLengthID(int length = 5)
    {
        // Generate a random positive number
        int randomID = Mathf.Abs(System.DateTime.Now.GetHashCode());

        // Convert the number to a string and truncate or pad it to the desired length
        string formattedID = randomID.ToString().PadLeft(length, '0').Substring(0, length);

        return formattedID;
    }


    public void EndGame(float finalDistance)
    {
        Debug.Log($"Recording score: Distance = {finalDistance}");
        string attemptID = GenerateFixedLengthID();
        ScoreDatabase.Instance.AddScore(int.Parse(attemptID), finalDistance);
    }


}
