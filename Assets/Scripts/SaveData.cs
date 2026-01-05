using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
	public int score;
	public int gridRows;
	public int gridColumns;

	public List<CardData> cards = new List<CardData>();
}

[Serializable]
public class CardData
{
	public int matchID;
	public int siblingIndex;
	public bool isMatched;
}
