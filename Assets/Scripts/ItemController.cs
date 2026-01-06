using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ItemController : MonoBehaviour, IPointerClickHandler
{
	[Header("Sprites")]
	public Sprite frontSprite;
	public Sprite backSprite;

	[Header("UI")]
	public Image cardImage;
	public RectTransform visualRoot;

	[HideInInspector] public int itemID;

	public bool isFlipped { get; private set; }
	public bool isMatched { get; set; }
	public bool isAnimating { get; private set; }

	public float flipDuration = 0.15f;

	void Awake()
	{
		if (cardImage == null)
			cardImage = GetComponentInChildren<Image>();

		if (visualRoot == null)
			visualRoot = cardImage.rectTransform;

		// IMPORTANT: DO NOT resize anything
		visualRoot.localScale = Vector3.one;

		ForceFlip(false);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!GameManager.Instance.CanInteract) return;
		if (isMatched || isFlipped || isAnimating) return;

		GameManager.Instance.SelectItem(this);
	}

	// ================= FLIP =================

	public void Flip()
	{
		if (isAnimating) return;

		StartCoroutine(FlipRoutine(!isFlipped));
	}

	public void ForceFlip(bool showFront)
	{
		StopAllCoroutines();
		isAnimating = false;
		isFlipped = showFront;
		cardImage.sprite = isFlipped ? frontSprite : backSprite;
		visualRoot.localScale = Vector3.one;
	}

	IEnumerator FlipRoutine(bool showFront)
	{
		isAnimating = true;

		float half = flipDuration / 2f;
		float t = 0f;

		// only play sound when flipping to front
		if(cardImage.sprite == backSprite)
			SoundManager.Instance?.PlayFlip();

		// shrink visual only
		while (t < half)
		{
			t += Time.deltaTime;
			float s = Mathf.Lerp(1f, 0f, t / half);
			visualRoot.localScale = new Vector3(s, 1f, 1f);
			yield return null;
		}

		isFlipped = showFront;
		cardImage.sprite = isFlipped ? frontSprite : backSprite;

		// expand visual only
		t = 0f;
		while (t < half)
		{
			t += Time.deltaTime;
			float s = Mathf.Lerp(0f, 1f, t / half);
			visualRoot.localScale = new Vector3(s, 1f, 1f);
			yield return null;
		}

		visualRoot.localScale = Vector3.one;
		isAnimating = false;
	}
}
