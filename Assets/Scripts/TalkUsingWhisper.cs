using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

namespace SIGVerse.TalkingWithPoodle
{
	public class TalkUsingWhisper: MonoBehaviour
	{
		private const int SamplingFrequency = 44100; //sampling frequency
		private const int MaxRecordingTime  = 60; //[s]

		public SAPISpeechSynthesis tts;

		public string micName = "Headset Microphone (Oculus Virtual Audio Device)";
		public TMP_Text statusText;

		public string openAiApiKey;
		public List<string> chatGptSystemContents;

		private AudioSource audioSource;
		private AudioClip audioClip;
		private float[] audioSamples = new float[1024];
		private bool canTalk = true;

		private InputDevice leftHandDevice;
		private InputDevice rightHandDevice;


		void Awake()
		{
			this.audioSource = this.GetComponent<AudioSource>();
		}

		void Start()
		{
			for(int i=0; i<Microphone.devices.Length; i++)
			{
				Debug.Log("Mic device["+(i+1)+"]: " + Microphone.devices[i]);
			}
			if(!Microphone.devices.Contains(this.micName))
			{
				this.micName = "null"; //null means default mic
			}

			Debug.Log("Mic Name: " + this.micName);

			StartCoroutine(GetXrDevice());
		}

		void Update()
		{
			AudioListener.GetOutputData(this.audioSamples, 1);
			float volume = this.audioSamples.Select(x => x*x).Sum() / this.audioSamples.Length;

			if (this.rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool aButton) && aButton) 
			{
				if(!Microphone.IsRecording(this.micName) && this.canTalk)
				{
					StartRecording();
				}
			}
			if (this.leftHandDevice .TryGetFeatureValue(CommonUsages.primaryButton, out bool xButton) && xButton) 
			{
				if(Microphone.IsRecording(this.micName))
				{
					StopRecording();
					StartCoroutine(TalkWithChatGpt(WavTools.ToWav(this.audioClip), this.openAiApiKey, this.chatGptSystemContents));
				}
			}
		}

		private IEnumerator GetXrDevice()
		{
			yield return StartCoroutine(GetXrDevice(XRNode.LeftHand,  x => this.leftHandDevice  = x));
			yield return StartCoroutine(GetXrDevice(XRNode.RightHand, x => this.rightHandDevice = x));

			this.statusText.text = "Please talk";
		}

		public static IEnumerator GetXrDevice(XRNode xrNode, Action<InputDevice> callback)
		{
			var devices = new List<InputDevice>();

			while(devices.Count != 1)
			{
				yield return new WaitForSecondsRealtime(1.0f);

				InputDevices.GetDevicesAtXRNode(xrNode, devices);

				if(devices.Count == 1)
				{
					callback(devices[0]);
					Debug.Log("Find XR Device. Name="+devices[0].name);
				}
				else if(devices.Count != 1)
				{
					Debug.LogWarning(xrNode.ToString()+" Count != 1 Count="+devices.Count);
				}
			}
		}

		private void StartRecording()
		{
			this.canTalk = false;

			Debug.Log("Recording Start");
			this.audioClip = Microphone.Start(this.micName, false, MaxRecordingTime, SamplingFrequency);
			this.statusText.text = "Recording...";
		}

		private void StopRecording()
		{
			int position = Microphone.GetPosition(this.micName);

			Microphone.End(this.micName);

			float[] soundData = new float[this.audioClip.samples * this.audioClip.channels];
			this.audioClip.GetData(soundData, 0);

			float[] clipData = new float[position * this.audioClip.channels];

			System.Array.Copy(soundData, clipData, clipData.Length);

			this.audioClip = AudioClip.Create(this.audioClip.name, position, this.audioClip.channels, this.audioClip.frequency, false);
			this.audioClip.SetData(clipData, 0);

			this.audioSource.clip = this.audioClip;
			this.statusText.text = "Stop";
			Debug.Log("Recording End");
		}

		private IEnumerator TalkWithChatGpt(byte[] wavData, string openAiApiKey, List<string> systemContents)
		{
			WhisperConnection whisperConn = new WhisperConnection(openAiApiKey);

			string whisperResponse=string.Empty;
			yield return StartCoroutine(whisperConn.ReceiveVoiceStringFromWhisper(wavData, "AudioClip.wav", (result)=>whisperResponse=result));

			Debug.Log("Whisper Response: "+whisperResponse);

			ChatGptConnection chatGptConn = new ChatGptConnection(openAiApiKey, systemContents);

			string chatGptResponse=string.Empty;
			this.statusText.text = "Sending your voice...";
			yield return StartCoroutine(chatGptConn.ReceiveReplyFromChatGpt(whisperResponse, (result) => chatGptResponse = result));
			this.statusText.text = "The poodle speaking";
			this.tts.Speak(chatGptResponse);

			Debug.Log("ChatGPT Response: "+chatGptResponse);

			while(this.tts.IsSpeaking())
			{
				yield return new WaitForSeconds(0.5f);
			}

			this.statusText.text = "Please talk";
			this.canTalk = true;
		}


		private void Play()
		{
			this.canTalk = false;

			Debug.Log("Play");
			this.statusText.text = "Playing...";
			this.audioSource.Play();
		}

		private IEnumerator CheckPlaying()
		{
			while (true)
			{
				yield return new WaitForSeconds(0.5f);

				if(!this.audioSource.isPlaying)
				{
					this.statusText.text = "Stop";
					this.canTalk = true;
					break;
				}
			}
		}
	}
}

