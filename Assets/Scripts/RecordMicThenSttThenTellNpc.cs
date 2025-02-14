using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class RecordMicThenSttThenTellNpc : MonoBehaviour
{
    // str get mic
    string selectedMicrophone = Microphone.devices[0];
    bool isRecording = false;
    AudioClip recordedClip;
    int recordingStartTime;
    LayerMask raycastLayerMask = LayerMask.GetMask("Character");
    RaycastHit? thingRaycastHit;
    RaycastHit NpcImTalkingTo;

    void Start() 
    {
        XRInputManager.Instance.OnRecordButtonPressed += HandleRecordButton;
    }

    void OnDestroy()
    {
        XRInputManager.Instance.OnRecordButtonPressed -= HandleRecordButton;
    }
    
    void StartRecording() 
    {
        // do recording
        Debug.Log("starting recording");

        recordedClip = Microphone.Start(selectedMicrophone, false, 600, 44100);
        recordingStartTime = Microphone.GetPosition(selectedMicrophone);
        isRecording = true;
    }

    string StopRecordingAndReturnTranscription()
    {
        Debug.Log("stopping recording");

        int endTime = Microphone.GetPosition(selectedMicrophone);
        isRecording = false;
        Microphone.End(selectedMicrophone);

        int recordingLength = endTime - recordingStartTime;
        if (recordingLength < 0) recordingLength += recordedClip.samples;

        float[] samples = new float[recordingLength];
        recordedClip.GetData(samples, 0);
        AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", recordingLength, 1, 44100, false);
        trimmedClip.SetData(samples, 0);

        recordedClip = trimmedClip;

        string transcriptionResult;

        StartCoroutine(SendWavToStt(transcription => 
        {
            if (transcription != null)
            {
                // Do something with the transcription here
                Debug.Log("Processing: " + transcription);
                transcriptionResult = transcription;
            }
        }));

        return transcriptionResult;
    }

    // send to wav
    void ClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
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

                foreach (float sample in samples)
                {
                    writer.Write((short)(sample * 32767));
                }
            }

            return stream.ToArray();
        }
    }

    // send to stt
    IEnumerator SendWavToStt(Action<string> onTranscriptionReceived) 
    {
        byte[] wavData = ClipToWav(recordedClip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "recorded_audio.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:8000/transcribe", form))
        {
            yield return www.SendWebRequest();

            // receive text back
            string transcription = www.downloadHandler.text;
            Debug.Log($"Transcribed text: {transcription}");
            onTranscriptionReceived?.Invoke(transcription); // Pass result
        }
    }

    // check if anyone is in front of you, probs a ray
    void FixedUpdate() 
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10, raycastLayerMask))
        { 
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow); 
            Debug.Log("Did Hit");

            thingRaycastHit = hit;

            if (hit.collider.gameObject.tag == "NPC")
            {
                Debug.Log("Raycast hit an NPC!");
                // Your NPC specific logic here (e.g., interact with NPC)
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10, Color.white);
            thingRaycastHit = null;
        }
    }

    void HandleRecordButton(bool isPressed, bool isLeftHand)
    {
        if (!raycastHitSomething)
        {
            Debug.Log("Raycast sees nothing")
            return
        }

        if (isPressed)
        {
            StartRecording();
            NpcImTalkingTo = thingRaycastHit;
        }
        else 
        {
            string transcription = StopRecordingAndReturnTranscription();
            TellNpcWhatISaid(transcription);
        }
    }

    // tell that person what you said
    void TellNpcWhatISaid(string words)
    {
        NpcImTalkingTo.collider.GetComponent<NPCController>();
    }

}