using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class MicrophoneRecorder : MonoBehaviour
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

    private IEnumerator SaveAndSendAudio() {
        Debug.Log("start save");

        byte[] wavData = SaveWav(recordedClip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "recorded_audio.wav", "audio/wav");

        using(UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8000/transcribe", form)) {
            yield return www.SendWebRequest();
            Debug.Log("sent");

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError(www.error);
            } else {
                string transcription = www.downloadHandler.text;
                Debug.Log($"Transcribed text: {transcription}");
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
