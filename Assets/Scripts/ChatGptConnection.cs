using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace SIGVerse.TalkingWithPoodle
{
	[Serializable]
	public class ChatGptMessage
	{
		public string role;
		public string content;
	}

	[Serializable]
	public class ChatGptRequest
	{
		public string model;
		public List<ChatGptMessage> messages;
	}

	[System.Serializable]
	public class ChatGptResponse
	{
		public string id;
		public string @object;
		public int created;
		public Choice[] choices;
		public Usage usage;

		[System.Serializable]
		public class Choice
		{
			public int index;
			public ChatGptMessage message;
			public string finish_reason;
		}

		[System.Serializable]
		public class Usage
		{
			public int prompt_tokens;
			public int completion_tokens;
			public int total_tokens;
		}
	}

	public class ChatGptConnection
	{
		private string model;
		private Dictionary<string, string> headerMap;

		private List<ChatGptMessage> messages = new();

		public ChatGptConnection(string apiKey, List<string> systemContents, string model="gpt-3.5-turbo")
		{
			this.model = model;

			this.headerMap = new Dictionary<string, string>
			{
				{"Authorization", "Bearer " + apiKey},
				{"Content-type", "application/json"},
				{"X-Slack-No-Retry", "1"}
			};

			foreach(string systemContent in systemContents)
			{
				this.messages.Add(new ChatGptMessage(){ role = "system",content = systemContent});
			}
		}

		public IEnumerator ReceiveReplyFromChatGpt(string userMessage, UnityAction<string> callback)
		{
			// ChatGPT API
			string url = "https://api.openai.com/v1/chat/completions";

//			Debug.Log("ChatGPT User Message="+userMessage);

			this.messages.Add(new ChatGptMessage { role = "user", content = userMessage });
			
			ChatGptRequest requestData = new()
			{
				model = this.model,
				messages = this.messages
			};

//			Debug.Log("ChatGPT Request JSON="+JsonUtility.ToJson(requestData));

			using UnityWebRequest webRequest = new UnityWebRequest(url, "POST")
			{
				uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestData))),
				downloadHandler = new DownloadHandlerBuffer()
			};

			foreach (KeyValuePair<string, string> header in headerMap)
			{
				webRequest.SetRequestHeader(header.Key, header.Value);
			}

			Debug.Log("Send Message to ChatGPT");
			yield return webRequest.SendWebRequest();
			Debug.Log("Received message from ChatGPT");

			if (webRequest.result == UnityWebRequest.Result.Success)
			{
				string responseJson = webRequest.downloadHandler.text;
				ChatGptResponse responseData = JsonUtility.FromJson<ChatGptResponse>(responseJson);
//				Debug.Log("ChatGPT:" + responseObject.choices[0].message.content);
				this.messages.Add(responseData.choices[0].message);
				callback(responseData.choices[0].message.content);
			}
			else
			{
				Debug.LogError("ChatGPT WebRequest.error:"+webRequest.error);
				Debug.LogError("ChatGPT WebRequest.error text:"+webRequest.downloadHandler.text);
			}
		}
	}
}

