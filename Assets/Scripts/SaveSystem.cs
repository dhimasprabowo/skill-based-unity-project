using System.IO;
using UnityEngine;

public static class SaveSystem
{
	private static string SavePath =>
		Path.Combine(Application.persistentDataPath, "save.json");

	public static void Save(SaveData data)
	{
		string json = JsonUtility.ToJson(data, true);
		File.WriteAllText(SavePath, json);
		Debug.Log($"Game Saved to {SavePath}");
	}

	public static SaveData Load()
	{
		if (!File.Exists(SavePath))
			return null;

		string json = File.ReadAllText(SavePath);
		return JsonUtility.FromJson<SaveData>(json);
	}

	public static void DeleteSave()
	{
		if (File.Exists(SavePath))
		{
			File.Delete(SavePath);
			Debug.LogWarning("Save file deleted due to mismatch or reset");
		}
	}
}
