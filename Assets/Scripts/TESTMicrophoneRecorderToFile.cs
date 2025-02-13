using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class TESTMicrophoneRecorderToFile : MonoBehaviour
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
    }

    void StopRecording()
    {
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

        SaveWav(recordedClip);
    }

    private void SaveWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        string fileName = $"Recording_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
        string filePath = Path.Combine(outputPath, fileName);

        using (FileStream fileStream = File.Create(filePath))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
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

            Debug.Log($"Recording saved to: {filePath}");
        }
    }
}
