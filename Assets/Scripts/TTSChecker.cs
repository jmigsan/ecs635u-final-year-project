using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System.Collections;

public class TTSChecker : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Playing TTS...")
            StartCoroutine(PlayTTS());
        }
    }
    
    IEnumerator PlayTTS()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        Task<AudioClip> ttsTask = GetTTS();
        
        while (!ttsTask.IsCompleted)
        {
            yield return null;
        }
        
        AudioClip ttsClip = ttsTask.Result;
        audioSource.clip = ttsClip;
        audioSource.Play();
    }

    async Task<AudioClip> GetTTS()
    {
        WWWForm form = new WWWForm();
        form.AddField("words", "Kamusta ka. Masaya ka ba?");

        using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8000/tts", form))
        {
            www.downloadHandler = new DownloadHandlerAudioClip(null, AudioType.MPEG);
            await www.SendWebRequest();
            return DownloadHandlerAudioClip.GetContent(www);
        }
    }
}