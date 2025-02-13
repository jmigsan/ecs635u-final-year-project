using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class TESTMicrophoneRecorderTTS : MonoBehaviour
{
    private AudioClip recordedClip;
    private bool isRecording = false;
    private int recordingStartTime;
    private string outputPath;

    void Start()
    {
        // Set the output path to a directory in your project
        outputPath = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(outputPath); // Create the directory if it doesn't exist
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleRecording();
        }
    }

    void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    void StartRecording()
    {
        recordedClip = Microphone.Start(null, false, 600, 44100);
        recordingStartTime = Microphone.GetPosition(null);
        isRecording = true;
        Debug.Log("start");
    }

    void StopRecording()
    {
        Debug.Log("stop");

        int endTime = Microphone.GetPosition(null);
        isRecording = false;
        Microphone.End(null);

        int recordingLength = endTime - recordingStartTime;
        if (recordingLength < 0) recordingLength += recordedClip.samples;

        float[] samples = new float[recordingLength];
        recordedClip.GetData(samples, 0);
        AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", recordingLength, 1, 44100, false);
        trimmedClip.SetData(samples, 0);

        recordedClip = trimmedClip;

        StartCoroutine(SaveAndSendAudio());
    }

    private IEnumerator SaveAndSendAudio()
    {
        Debug.Log("Preparing audio for sending...");

        // Convert the recorded clip to WAV data
        byte[] wavData = SaveWav(recordedClip);

        // Prepare form with the WAV data
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "recorded_audio.wav", "audio/wav");

        // Post the audio to your local API
        using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8000/transcribe", form))
        {
            yield return www.SendWebRequest();
            Debug.Log("API request sent");

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("API Error: " + www.error);
            }
            else
            {
                // Get the returned MP3 data (Edge TTS synthesis)
                byte[] mp3Data = www.downloadHandler.data;
                // Save the MP3 data to a temporary file
                string tempMp3Path = Path.Combine(Application.temporaryCachePath, "ttsAudio.mp3");
                File.WriteAllBytes(tempMp3Path, mp3Data);
                Debug.Log("MP3 file saved to: " + tempMp3Path);

                // Load the MP3 file as an AudioClip
                using (UnityWebRequest wwwAudio = UnityWebRequestMultimedia.GetAudioClip("file://" + tempMp3Path, AudioType.MPEG))
                {
                    yield return wwwAudio.SendWebRequest();

                    if (wwwAudio.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("AudioClip Error: " + wwwAudio.error);
                    }
                    else
                    {
                        AudioClip ttsClip = DownloadHandlerAudioClip.GetContent(wwwAudio);
                        Debug.Log("Playing TTS audio");
                        
                        // Play the clip using an AudioSource on this GameObject
                        AudioSource audioSource = GetComponent<AudioSource>();
                        audioSource.clip = ttsClip;
                        audioSource.Play();
                    }
                }
            }
        }
    }


    byte[] SaveWav(AudioClip clip)
    {
        Debug.Log("save wav");

        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write WAV header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write(44100);
                writer.Write(44100 * 2);
                writer.Write((ushort)2);
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                // Write samples
                foreach (float sample in samples)
                {
                    writer.Write((short)(sample * 32767));
                }
            }

            return stream.ToArray();
        }
    }
}
