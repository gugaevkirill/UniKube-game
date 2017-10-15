using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class Health : NetworkBehaviour {
	private const string url = "localhost:8893/";

	public const int maxHealth = 100;

	[SyncVar(hook = "OnChangeHealth")]
	public int currentHealth = maxHealth;

	public RectTransform healthBar;

	// Id текущего игрока
	private string playerId = "";

	public AnimationClip animDie;

	void Start()
	{
		if (!isServer) {
			Debug.Log("I'm not a host, miss Health.cs constructor");
			return;
		}

		InvokeRepeating("ApiHP", 2.0f, 0.5f);
		Init();
	}

	/**
	 * Отправляет запрос и выполняет колбек если есть
	 */
	IEnumerator sendRequest(string method, string player = "", System.Action<string> callback = null)
	{
		string fullUrl = url + method + "/";
		if (!string.IsNullOrEmpty(player)) {
			fullUrl += player + "/";
		}

		Debug.Log(fullUrl);

		UnityWebRequest request = UnityWebRequest.Get(fullUrl);        
		yield return request.Send();

		if (request.isNetworkError)
		{
			// Show error results as text        
			Debug.Log("Error! We received: " + request.downloadHandler.text);
		} else if (callback != null) {
			Debug.Log (request.downloadHandler.text);
			callback (request.downloadHandler.text);
		}
	}

	/**
	 * Создает или обновляет счетчик ХП для игрока
	 */
	public void Init()
	{
		StartCoroutine(sendRequest(
			"init",
			playerId,
			delegate (string jsonResponse) {
				InitResponse initRes = JsonUtility.FromJson<InitResponse>(jsonResponse);
				playerId = initRes.player_id;
			}
		));
	}

	public void TakeDamage()
	{
		if (!isServer) {
			return;
		}

		StartCoroutine(sendRequest("damage", playerId));
	}

	void ApiHP()
	{
		Debug.Log ("ApiHP " + playerId);
		StartCoroutine(sendRequest(
			"state",
			playerId,
			delegate (string jsonResponse) {
				HPResponse hpRes = JsonUtility.FromJson<HPResponse>(jsonResponse);
				currentHealth = hpRes.hp;

				if (currentHealth <= 0) {
					Die();
				}
			}
		));
	}

	void OnChangeHealth (int currentHealth )
	{
		healthBar.sizeDelta = new Vector2(currentHealth , healthBar.sizeDelta.y);
	}

	void Die()
	{
		Init ();
	}
}

public class InitResponse
{
	public string player_id;
	public string result;
}

public class HPResponse
{
	public string player_id;
	public int hp;
	public string result;
}
