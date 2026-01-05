using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
			gridColumns += 1;
	}

	void Start()
	{
		Score = 0;
		Combo = 0;
		UpdateUI();

		StartCoroutine(SetupLayoutAndStartGame());
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
		}

		isProcessingMatch = false;
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
		if (comboText) comboText.text = Combo > 0 ? $"x{Combo}" : "";
	}
}
