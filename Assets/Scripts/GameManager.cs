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

	[Header("Load Confirmation UI")]
	public GameObject loadConfirmPanel;
	public Button loadButton;
	public Button newGameButton;

	public bool CanInteract { get; private set; }
	public int Score { get; private set; }
	public int Combo { get; private set; }

	private Queue<ItemController> matchQueue = new Queue<ItemController>();
	private bool isProcessingMatch;
	private readonly List<ItemController> allCards = new List<ItemController>();

	private SaveData pendingSave;

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
		if (restartButton)
			restartButton.onClick.AddListener(RestartGame);

		if (loadButton)
			loadButton.onClick.AddListener(OnLoadConfirmed);

		if (newGameButton)
			newGameButton.onClick.AddListener(OnNewGameConfirmed);

		if (levelCompletePanel)
			levelCompletePanel.SetActive(false);

		if (loadConfirmPanel)
			loadConfirmPanel.SetActive(false);

		SaveData save = SaveSystem.Load();

		if (save != null)
		{
			if (save.gridRows != gridRows || save.gridColumns != gridColumns)
			{
				Debug.LogError("Grid size mismatch. Save file deleted.");
				SaveSystem.DeleteSave();
				StartNewGame();
			}
			else
			{
				// Valid save → wait for player choice
				pendingSave = save;
				loadConfirmPanel.SetActive(true);
				CanInteract = false;
			}
		}
		else
		{
			StartNewGame();
		}
	}

	void OnDestroy()
	{
		if (restartButton)
			restartButton.onClick.RemoveListener(RestartGame);
	}

	void OnApplicationQuit()
	{
		SaveGame();
	}

	// ================= LOAD CONFIRM =================

	void OnLoadConfirmed()
	{
		loadConfirmPanel.SetActive(false);
		LoadGame(pendingSave);
		pendingSave = null;
	}

	void OnNewGameConfirmed()
	{
		loadConfirmPanel.SetActive(false);
		SaveSystem.DeleteSave();
		StartNewGame();
		pendingSave = null;
	}

	// ================= START / LOAD =================

	void StartNewGame()
	{
		Score = 0;
		Combo = 0;
		UpdateUI();
		StartCoroutine(SetupLayoutAndStartGame());
	}

	void LoadGame(SaveData data)
	{
		if (data == null)
			return;

		Score = data.score;
		Combo = 0;
		UpdateUI();
		StartCoroutine(LoadRoutine(data));
	}

	IEnumerator SetupLayoutAndStartGame()
	{
		yield return null;

		SetupGridConstraint();
		FitGridToContainer();
		SpawnCards();
		StartCoroutine(PreviewSequence());
	}

	IEnumerator LoadRoutine(SaveData data)
	{
		yield return null;

		SetupGridConstraint();
		FitGridToContainer();

		List<CardData> sortedCards = data.cards
			.OrderBy(c => c.siblingIndex)
			.ToList();

		foreach (var cd in sortedCards)
		{
			ItemController card = Instantiate(cardPrefab, gridLayout.transform);

			Sprite front = cardFrontSprites
				.First(s => s.GetInstanceID() == cd.matchID);

			card.frontSprite = front;
			card.backSprite = cardBack;
			card.itemID = cd.matchID;
			card.isMatched = cd.isMatched;

			card.ForceFlip(cd.isMatched);
			allCards.Add(card);
		}

		CanInteract = true;
	}

	// ================= GRID =================

	void SetupGridConstraint()
	{
		gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
		gridLayout.constraintCount = gridRows;
	}

	void FitGridToContainer()
	{
		Rect rect = gameContainer.rect;

		RectOffset padding = gridLayout.padding;
		Vector2 spacing = gridLayout.spacing;

		float spacingY = spacing.y * (gridRows - 1);
		float availableHeight = rect.height - padding.top - padding.bottom - spacingY;
		float cellHeight = availableHeight / gridRows;

		float aspect = 3.5f / 4f;
		float cellWidth = cellHeight * aspect;

		float spacingX = spacing.x * (gridColumns - 1);
		float availableWidth = rect.width - padding.left - padding.right - spacingX;

		if (cellWidth * gridColumns > availableWidth)
		{
			cellWidth = availableWidth / gridColumns;
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
			SaveGame();

			if (allCards.All(c => c.isMatched))
				ShowLevelComplete();
		}

		isProcessingMatch = false;
	}

	// ================= SAVE =================

	public void SaveGame()
	{
		if (allCards.Count == 0)
			return;

		// Do not save unless there is real progress
		if (!allCards.Any(c => c.isMatched))
			return;

		SaveData data = new SaveData
		{
			score = Score,
			gridRows = gridRows,
			gridColumns = gridColumns
		};

		foreach (var card in allCards)
		{
			data.cards.Add(new CardData
			{
				matchID = card.itemID,
				siblingIndex = card.transform.GetSiblingIndex(),
				isMatched = card.isMatched
			});
		}

		SaveSystem.Save(data);
	}


	// ================= RESTART =================

	public void RestartGame()
	{
		StopAllCoroutines();
		matchQueue.Clear();
		isProcessingMatch = false;
		CanInteract = false;

		foreach (var card in allCards.ToList())
			Destroy(card.gameObject);

		allCards.Clear();

		SaveSystem.DeleteSave();

		Score = 0;
		Combo = 0;
		UpdateUI();

		StartCoroutine(SetupLayoutAndStartGame());

		if (levelCompletePanel)
			levelCompletePanel.SetActive(false);
	}

	// ================= UI =================

	void ShowLevelComplete()
	{
		CanInteract = false;
		StopAllCoroutines();

		if (levelCompletePanel)
			levelCompletePanel.SetActive(true);

		if (levelCompleteScoreText)
			levelCompleteScoreText.text = Score.ToString();
	}

	void UpdateUI()
	{
		if (scoreText) scoreText.text = Score.ToString();
		if (comboText) comboText.text = Combo > 1 ? $"Combo x{Combo}!" : "";
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
}
