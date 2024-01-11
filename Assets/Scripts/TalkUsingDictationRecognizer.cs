using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.XR;

namespace SIGVerse.TalkingWithPoodle
{
	public class TalkUsingDictationRecognizer: MonoBehaviour
	{
		public SAPISpeechSynthesis tts;

//		public string micName = "Headset Microphone (Oculus Virtual Audio Device)";
		public TMP_Text statusText;

		public string openAiApiKey;
		public List<string> chatGptSystemContents;

		public TMP_Text humanSpeechText;
		public TMP_Text poodleSpeechText;

		private DictationRecognizer dictationRecognizer;

		private bool canTalk = true;

		void Start()
		{
			for(int i=0; i<Microphone.devices.Length; i++)
			{
				Debug.Log("Mic device["+(i+1)+"]: " + Microphone.devices[i]);
			}

			InitDictationRecognizer();

			this.humanSpeechText.text = string.Empty;
			this.poodleSpeechText.text = string.Empty;

			this.dictationRecognizer.Start();

			StartCoroutine(CheckSpeechSystemStatus());
		}

		private void InitDictationRecognizer()
		{
			this.dictationRecognizer = new DictationRecognizer();

			this.dictationRecognizer.AutoSilenceTimeoutSeconds = 1000f;
			this.dictationRecognizer.InitialSilenceTimeoutSeconds = 1000f;

			this.dictationRecognizer.DictationResult += (text, confidence) =>
			{
				Debug.LogFormat("Dictation result: {0}", text);
				if(this.canTalk)
				{
					StartCoroutine(TalkWithChatGpt(text, this.openAiApiKey, this.chatGptSystemContents));
				}
				else
				{
					Debug.LogWarningFormat("Could not send message.: {0}", text);
				}
			};

			//this.dictationRecognizer.DictationHypothesis += (text) =>
			//{
			//	Debug.LogFormat("Dictation hypothesis: {0}", text);
			//	this.hypothesesText.text += text;
			//};

			this.dictationRecognizer.DictationComplete += (completionCause) =>
			{
				if (completionCause != DictationCompletionCause.Complete)
				{
					Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
				}
				this.dictationRecognizer.Start();
			};

			this.dictationRecognizer.DictationError += (error, hresult) =>
			{
				Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
			};
		}

		private IEnumerator CheckSpeechSystemStatus()
		{
			while (this.dictationRecognizer.Status!=SpeechSystemStatus.Running)
			{
				yield return new WaitForSeconds(0.3f);
			}

			Debug.LogFormat("Speech System Status: {0}", this.dictationRecognizer.Status);
			this.canTalk = true;
			this.statusText.text = "Please talk";

			SpeechSystemStatus previousStatus = this.dictationRecognizer.Status;

			while (true)
			{
				yield return new WaitForSeconds(1f);

				if(previousStatus != this.dictationRecognizer.Status)
				{
					Debug.LogFormat("Speech System Status: {0}", this.dictationRecognizer.Status);

					previousStatus = this.dictationRecognizer.Status;
				}
			}
		}

		private void OnDestroy()
		{
			if (this.dictationRecognizer.Status == SpeechSystemStatus.Running)
			{
				this.dictationRecognizer.Stop();
			}
			this.dictationRecognizer.Dispose();
		}

		private IEnumerator TalkWithChatGpt(string dictationResult, string openAiApiKey, List<string> systemContent)
		{
			this.canTalk = false;

			ChatGptConnection chatGptConn = new ChatGptConnection(openAiApiKey, systemContent);

			this.statusText.text = "Sending your voice...";
			this.humanSpeechText.text = dictationResult;

			string chatGptResponse=string.Empty;
			yield return StartCoroutine(chatGptConn.ReceiveReplyFromChatGpt(dictationResult, (result) => chatGptResponse = result));
			
			this.statusText.text = "The poodle speaking";
			this.poodleSpeechText.text = chatGptResponse;
			this.tts.Speak(chatGptResponse);

			Debug.Log("ChatGPT Response: "+chatGptResponse);

			while(this.tts.IsSpeaking())
			{
				yield return new WaitForSeconds(0.5f);
			}

			this.statusText.text = "Please talk";
			this.canTalk = true;
		}
	}
}

