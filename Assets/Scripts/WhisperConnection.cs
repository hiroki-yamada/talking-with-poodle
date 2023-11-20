using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace SIGVerse.TalkingWithPoodle
{
	[Serializable]
	public class WhisperResponse
	{
		public string text;
	}
	
	public class WhisperConnection
	{
		private string model;
		private Dictionary<string, string> headerMap;

		public WhisperConnection(string apiKey, string model="whisper-1")
		{
			this.model = model;

			this.headerMap = new Dictionary<string, string>
			{
				{"Authorization", "Bearer " + apiKey}
			};
		}

		public IEnumerator ReceiveVoiceStringFromWhisper(byte[] wavData, string fileName, UnityAction<string> callback)
		{
			// Whisper API
			string url = "https://api.openai.com/v1/audio/transcriptions";

			WWWForm wwwForm = new();
			wwwForm.AddField("model", this.model);
			wwwForm.AddBinaryData("file", wavData, fileName, "multipart/form-data");

			using UnityWebRequest webRequest = UnityWebRequest.Post(url, wwwForm);
			webRequest.downloadHandler = new DownloadHandlerBuffer();

			foreach (KeyValuePair<string, string> header in this.headerMap)
			{
				webRequest.SetRequestHeader(header.Key, header.Value);
			}

			yield return webRequest.SendWebRequest();

			if (webRequest.result == UnityWebRequest.Result.Success)
			{
				WhisperResponse responseData = JsonUtility.FromJson<WhisperResponse>(webRequest.downloadHandler.text);
				callback(responseData.text);
			}
			else
			{
				Debug.LogError("Whisper WebRequest.error:"+webRequest.error);
				Debug.LogError("Whisper WebRequest.error text:"+webRequest.downloadHandler.text);
			}
		}
	}
}

