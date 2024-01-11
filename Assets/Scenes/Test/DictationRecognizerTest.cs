using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using TMPro;
using System.Collections;

public class DictationRecognizerTest : MonoBehaviour
{
	[SerializeField]
	private TMP_Text hypothesesText;

	[SerializeField]
	private TMP_Text recognitionsText;

	private DictationRecognizer dictationRecognizer;

	void Start()
	{
		for(int i=0; i<Microphone.devices.Length; i++)
		{
			Debug.Log("Mic device["+(i+1)+"]: " + Microphone.devices[i]);
		}

		InitDictationRecognizer();

		this.dictationRecognizer.Start();

		StartCoroutine(PrintSpeechSystemStatus());
	}

	private void InitDictationRecognizer()
	{
		this.dictationRecognizer = new DictationRecognizer();

		this.dictationRecognizer.AutoSilenceTimeoutSeconds = 1000f;
		this.dictationRecognizer.InitialSilenceTimeoutSeconds = 1000f;

		this.dictationRecognizer.DictationResult += (text, confidence) =>
		{
			Debug.LogFormat("Dictation result: {0}", text);
			this.recognitionsText.text += text + "\n";
		};

		this.dictationRecognizer.DictationHypothesis += (text) =>
		{
			Debug.LogFormat("Dictation hypothesis: {0}", text);
			this.hypothesesText.text += text;
		};

		this.dictationRecognizer.DictationComplete += (completionCause) =>
		{
			if (completionCause != DictationCompletionCause.Complete)
				Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
		};

		this.dictationRecognizer.DictationError += (error, hresult) =>
		{
			Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
		};
	}

	private IEnumerator PrintSpeechSystemStatus()
	{
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
}
