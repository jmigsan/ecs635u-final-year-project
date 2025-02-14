using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class TTSChecker : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Playing TTS...");
            StartCoroutine(PlayTTS());
        }
    }

    class TtsQuery
    {
        string words;
    }

    IEnumerator PlayTTS()
    {
        string wordsToSend = "Kamusta ka. Masaya ka ba?"; 

        TtsQuery query = new TtsQuery { words = wordsToSend };
        string jsonQuery = JsonUtility.ToJson(query);

        using (UnityWebRequest www = new UnityWebRequest("http://127.0.0.1:8000/tts", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonQuery);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(www.url, AudioType.MPEG);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            AudioSource audioSource = GetComponent<AudioSource>();
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}