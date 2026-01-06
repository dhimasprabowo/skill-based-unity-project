using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmPopup : MonoBehaviour
{
	[Header("UI")]
	public TMP_Text messageText;

	public Button yesButton;
	public TMP_Text yesLabel;

	public Button noButton;
	public TMP_Text noLabel;

	private Action onYes;
	private Action onNo;

	private bool initialized;

	void OnEnable()
	{
		if (initialized)
			return;

		if (yesButton)
			yesButton.onClick.AddListener(HandleYes);

		if (noButton)
			noButton.onClick.AddListener(HandleNo);

		initialized = true;
	}

	public void Show(
		string message,
		Action yesCallback,
		Action noCallback = null,
		string yesText = "Yes",
		string noText = "No"
	)
	{
		messageText.text = message;

		onYes = yesCallback;
		onNo = noCallback;

		if (yesCallback != null)
		{
			yesButton.gameObject.SetActive(true);
			if (yesLabel) yesLabel.text = yesText;
		}
		else
		{
			yesButton.gameObject.SetActive(false);
		}

		if (noCallback != null)
		{
			noButton.gameObject.SetActive(true);
			if (noLabel) noLabel.text = noText;
		}
		else
		{
			noButton.gameObject.SetActive(false);
		}

		gameObject.SetActive(true);
	}

	public void Hide()
	{
		gameObject.SetActive(false);
		onYes = null;
		onNo = null;
	}

	void HandleYes()
	{
		onYes?.Invoke();
		Hide();
	}

	void HandleNo()
	{
		onNo?.Invoke();
		Hide();
	}
}
