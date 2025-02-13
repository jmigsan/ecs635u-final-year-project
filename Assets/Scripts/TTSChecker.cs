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

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("http://127.0.0.1:8000/tts", AudioType.MPEG))
        {
            www.method = "POST";
            www.uploadHandler = new UploadHandlerRaw(form.data);
            www.downloadHandler = new DownloadHandlerAudioClip(null, AudioType.MPEG);

            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return DownloadHandlerAudioClip.GetContent(www);
        }
    }
}