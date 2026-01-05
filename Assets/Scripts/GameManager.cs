using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	[Header("Card Setup")]
	public ItemController cardPrefab;
	public Sprite cardBack;
	public List<Sprite> cardFrontSprites;

	[Header("Grid Container")]
	public RectTransform gameContainer;
	public GridLayoutGroup gridLayout;

	[Header("Grid Size")]
	[Min(1)] public int gridRows = 3;
	[Min(1)] public int gridColumns = 4;

	[Header("Preview")]
	public float previewDelay = 1f;

	[Header("Scoring")]
	public int baseMatchScore = 100;

	[Header("UI")]
	public TMP_Text scoreText;
	public TMP_Text comboText;
    public Button restartButton;
    public GameObject levelCompletePanel;
    public TMP_Text levelCompleteScoreText;

	public bool CanInteract { get; private set; }
	public int Score { get; private set; }
	public int Combo { get; private set; }

	private Queue<ItemController> matchQueue = new Queue<ItemController>();
	private bool isProcessingMatch;
	private readonly List<ItemController> allCards = new List<ItemController>();

	int TotalCards => gridRows * gridColumns;
	int MatchCount => TotalCards / 2;

	// ================= UNITY =================

	void Awake()
	{
		Instance = this;
	}

	void OnValidate()
	{
		if ((gridRows * gridColumns) % 2 != 0)
		{
			Debug.LogError("Grid is not even, auto fixed column to make the grid even!");
			gridColumns += 1;
		}
	}

	void Start()
	{
		Score = 0;
		Combo = 0;
		UpdateUI();

		if (restartButton != null)
		{
			restartButton.onClick.AddListener(RestartGame);
		}

		if (levelCompletePanel != null)
		{
			levelCompletePanel.SetActive(false);
		}

		StartCoroutine(SetupLayoutAndStartGame());
	}

	void OnDestroy()
	{
		if (restartButton != null)
		{
			restartButton.onClick.RemoveListener(RestartGame);
		}
	}

	IEnumerator SetupLayoutAndStartGame()
	{
		yield return null;

		SetupGridConstraint();
		FitGridToContainer();
		SpawnCards();
		StartCoroutine(PreviewSequence());
	}

	// ================= GRID =================

	void SetupGridConstraint()
	{
		gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
		gridLayout.constraintCount = gridRows;
	}

	void FitGridToContainer()
	{
		Rect containerRect = gameContainer.rect;

		int rows = gridRows;
		int columns = gridColumns;

		RectOffset padding = gridLayout.padding;
		Vector2 spacing = gridLayout.spacing;

		float totalSpacingY = spacing.y * (rows - 1);

		float availableHeight =
			containerRect.height -
			padding.top -
			padding.bottom -
			totalSpacingY;

		float cellHeight = availableHeight / rows;

		float aspect = 3.5f / 4f;
		float cellWidth = cellHeight * aspect;

		float totalSpacingX = spacing.x * (columns - 1);

		float availableWidth =
			containerRect.width -
			padding.left -
			padding.right -
			totalSpacingX;

		float totalGridWidth = cellWidth * columns;

		if (totalGridWidth > availableWidth)
		{
			cellWidth = availableWidth / columns;
			cellHeight = cellWidth / aspect;
		}

		gridLayout.cellSize = new Vector2(cellWidth, cellHeight);

		LayoutRebuilder.ForceRebuildLayoutImmediate(
			gridLayout.GetComponent<RectTransform>()
		);
	}

	// ================= SPAWN =================

	void SpawnCards()
	{
		int totalCards = TotalCards;

		if (cardFrontSprites.Count < MatchCount)
		{
			Debug.LogError("Not enough card front sprites!");
			return;
		}

		List<Sprite> pool = new List<Sprite>(cardFrontSprites);
		Shuffle(pool);

		List<Sprite> spawnSprites = new List<Sprite>();

		for (int i = 0; i < MatchCount; i++)
		{
			spawnSprites.Add(pool[i]);
			spawnSprites.Add(pool[i]);
		}

		Shuffle(spawnSprites);

		foreach (Sprite sprite in spawnSprites)
		{
			ItemController card = Instantiate(cardPrefab, gridLayout.transform);
			card.frontSprite = sprite;
			card.backSprite = cardBack;
			card.itemID = sprite.GetInstanceID();
			allCards.Add(card);
		}
	}

	// ================= PREVIEW =================

	IEnumerator PreviewSequence()
	{
		CanInteract = false;

		foreach (var card in allCards)
			card.ForceFlip(true);

		yield return new WaitForSeconds(previewDelay);

		foreach (var card in allCards)
			card.Flip();

		CanInteract = true;
	}

	// ================= INPUT =================

	public void SelectItem(ItemController item)
	{
		if (matchQueue.Contains(item)) return;

		item.Flip();
		matchQueue.Enqueue(item);

		if (!isProcessingMatch)
			StartCoroutine(ProcessQueue());
	}

	// ================= MATCH QUEUE =================

	IEnumerator ProcessQueue()
	{
		isProcessingMatch = true;

		while (matchQueue.Count >= 2)
		{
			ItemController a = matchQueue.Dequeue();
			ItemController b = matchQueue.Dequeue();

			if (a.isMatched || b.isMatched)
				continue;

			yield return new WaitForSeconds(0.4f);

			if (a.itemID == b.itemID)
			{
				a.isMatched = true;
				b.isMatched = true;

				Combo++;
				Score += baseMatchScore * Combo;
			}
			else
			{
				Combo = 0;
				a.Flip();
				b.Flip();
			}

			UpdateUI();

			// If every card is matched, show level complete UI
			if (allCards.Count > 0 && allCards.All(c => c.isMatched))
			{
				ShowLevelComplete();
			}
		}

		isProcessingMatch = false;
	}

	// ================= RESTART =================

	public void RestartGame()
	{
		// Stop any running coroutines and clear state
		StopAllCoroutines();
		matchQueue.Clear();
		isProcessingMatch = false;
		CanInteract = false;

		// Destroy all spawned cards
		foreach (var card in allCards.ToList())
		{
			if (card != null)
				Destroy(card.gameObject);
		}
		allCards.Clear();

		// Reset score/combo and UI
		Score = 0;
		Combo = 0;
		UpdateUI();

		// Restart setup
		StartCoroutine(SetupLayoutAndStartGame());

		// Hide level complete UI if present
		if (levelCompletePanel != null)
			levelCompletePanel.SetActive(false);
	}

	void ShowLevelComplete()
	{
		// prevent further interaction
		CanInteract = false;
		StopAllCoroutines();

		if (levelCompletePanel != null)
			levelCompletePanel.SetActive(true);

		if (levelCompleteScoreText != null)
			levelCompleteScoreText.text = Score.ToString();
	}

	// ================= UTIL =================

	void Shuffle<T>(List<T> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			int r = Random.Range(i, list.Count);
			(list[i], list[r]) = (list[r], list[i]);
		}
	}

	void UpdateUI()
	{
		if (scoreText) scoreText.text = Score.ToString();
		if (comboText) comboText.text = Combo > 1 ? $"Combo x{Combo}!" : "";
	}
}
