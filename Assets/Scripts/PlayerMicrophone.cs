using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerMicrophone : MonoBehaviour
{
    public static PlayerMicrophone Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // str get mic
    string selectedMicrophone;
    bool isRecording = false;
    AudioClip recordedClip;
    int recordingStartTime;
    LayerMask raycastLayerMask;
    RaycastHit thingRaycastHit;
    RaycastHit NpcImTalkingTo;
    GameObject lastHitObject = null;

    public TextMeshPro userSubtitles;

    public event Action<bool> PlayerIsTalking;

    void Start()
    {
        selectedMicrophone = Microphone.devices[0];
        Debug.Log(selectedMicrophone);

        raycastLayerMask = LayerMask.GetMask("NPC");

        XRInputManager.Instance.AButtonPressed += HandleRecordButton;

        userSubtitles.text = "";
    }

    void OnDestroy()
    {
        XRInputManager.Instance.AButtonPressed -= HandleRecordButton;
    }

    void StartRecording()
    {
        // do recording
        Debug.Log("starting recording");

        recordedClip = Microphone.Start(selectedMicrophone, false, 600, 44100);
        recordingStartTime = Microphone.GetPosition(selectedMicrophone);
        isRecording = true;
    }

    void StopRecording()
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

        StartCoroutine(SendWavToStt());
    }

    // send to wav
    byte[] ClipToWav(AudioClip clip)
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

    public class TranscriptionResponse
    {
        public string transcription;
    }

    // send to stt
    IEnumerator SendWavToStt()
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
            TranscriptionResponse data = JsonUtility.FromJson<TranscriptionResponse>(transcription);
            TellNpcWhatISaid(data.transcription); // This is in a weird spot. I don't like it. I should refactor this code. Be better organised.
        }
    }

    // check if anyone is in front of you, probs a ray
    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 20, raycastLayerMask))
        {
            // Debug.Log("Did Hit");
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            thingRaycastHit = hit;

            DisableScriptOnObject(lastHitObject);
            lastHitObject = hit.collider.gameObject;
            EnableScriptOnObject(lastHitObject);

        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 20, Color.white);
            thingRaycastHit = hit;

            DisableScriptOnObject(lastHitObject);
            lastHitObject = null;
        }
    }

    void EnableScriptOnObject(GameObject obj)
    {
        if (obj != null)
        {
            Component component = obj.GetComponent(Type.GetType("OutlineNoChildren"));
            if (component != null && component is MonoBehaviour)
            {
                ((MonoBehaviour)component).enabled = true;
            }
        }
    }
    
    void DisableScriptOnObject(GameObject obj)
    {
        if (obj != null)
        {
            Component component = obj.GetComponent(Type.GetType("OutlineNoChildren"));
            if (component != null && component is MonoBehaviour)
            {
                ((MonoBehaviour)component).enabled = false;
            }
        }
    }

    void HandleRecordButton(bool isPressed)
    {
        if (isPressed)
        {
            if (thingRaycastHit.collider != null)
            {
                NpcImTalkingTo = thingRaycastHit;
                StartRecording();
                PlayerIsTalking?.Invoke(isPressed);
            }
        }
        else
        {
            if (isRecording)
            {
                StopRecording();
                PlayerIsTalking?.Invoke(isPressed);
            }
        }
    }

    // tell that person what you said
    void TellNpcWhatISaid(string words)
    {
        string wordsFormatted = "You: " + words;
        StartCoroutine(DisplayUserSubtitles(wordsFormatted));

        GameObject npcObject = NpcImTalkingTo.collider.gameObject;

        DirectableNpc directableNpc = npcObject.GetComponent<DirectableNpc>();
        if (directableNpc != null)
        {
            Debug.Log("Talking to directable npc");
            if (directableNpc.inDirectorScene)
            {
                // It directly calls the network manager from the NPC. Then adds the player's input to the conversation.
                directableNpc.currentSceneDirector.SendPlayerInterruption(words, directableNpc.npcName);
            }
            else
            {
                directableNpc.Listen(words);
            }
        }

        ShopkeeperNpc shopkeeperNpc = npcObject.GetComponent<ShopkeeperNpc>();
        if (shopkeeperNpc != null)
        {
            Debug.Log("Talking to shopkeeper npc");
            shopkeeperNpc.Listen(words);
        }

        FollowerNpc followerNpc = npcObject.GetComponent<FollowerNpc>();
        if (followerNpc != null)
        {
            Debug.Log("Talking to follower npc");
            followerNpc.Listen(words);
        }
    }

    IEnumerator DisplayUserSubtitles(string words)
    {
        userSubtitles.text = words;
        yield return new WaitForSeconds(5);
        userSubtitles.text = "";
    }
}
