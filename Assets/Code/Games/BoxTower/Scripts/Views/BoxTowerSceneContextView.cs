using Disposable;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Code.Core.ShortGamesCore.Game2
{
public class BoxTowerSceneContextView : MonoBehaviourDisposable
{
	[Header("Game Objects")]
	[SerializeField]
	private Transform towerRoot;

	[SerializeField]
	private Camera mainCamera;

	[SerializeField]
	private ColorManager colorManager;

	[Header("Prefabs")]
	[SerializeField]
	private GameObject blockPrefab;

	[SerializeField]
	private GameObject chunkPrefab;

	[Header("HUD Elements")]
	[SerializeField]
	private TextMeshProUGUI scoreText;

	[SerializeField]
	private TextMeshProUGUI bestScoreText;

	[SerializeField]
	private Button pauseButton;

	[Header("Game Over Panel")]
	[SerializeField]
	private GameObject gameOverPanel;

	[SerializeField]
	private TextMeshProUGUI finalScoreText;

	[SerializeField]
	private TextMeshProUGUI finalBestScoreText;

	[SerializeField]
	private Button restartButton;

	[Header("Tap to Play")]
	[SerializeField]
	private GameObject tapToPlayPanel;

	[SerializeField]
	private Button tapToPlayButton;

	[Header("Tutorial")]
	[SerializeField]
	private GameObject tutorialPanel;

	[SerializeField]
	private TextMeshProUGUI tutorialText;

	[Header("Input")]
	[SerializeField]
	private Button fullScreenTapButton;

	// Properties
	public Transform TowerRoot => towerRoot;
	public Camera MainCamera => mainCamera;
	public ColorManager ColorManager => colorManager;
	public GameObject BlockPrefab => blockPrefab;
	public GameObject ChunkPrefab => chunkPrefab;

	// UI Properties
	public TextMeshProUGUI ScoreText => scoreText;
	public TextMeshProUGUI BestScoreText => bestScoreText;
	public Button PauseButton => pauseButton;

	public GameObject GameOverPanel => gameOverPanel;
	public TextMeshProUGUI FinalScoreText => finalScoreText;
	public TextMeshProUGUI FinalBestScoreText => finalBestScoreText;
	public Button RestartButton => restartButton;

	public GameObject TapToPlayPanel => tapToPlayPanel;
	public Button TapToPlayButton => tapToPlayButton;

	public GameObject TutorialPanel => tutorialPanel;
	public TextMeshProUGUI TutorialText => tutorialText;

	public Button FullScreenTapButton => fullScreenTapButton;

	private void Awake()
	{
		// Rotate tower root 45 degrees for diagonal view
		if (towerRoot != null)
		{
			towerRoot.rotation = Quaternion.Euler(0f, 45f, 0f);
		}
	}
}
}