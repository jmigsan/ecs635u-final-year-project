using UnityEngine;

public class PlayerInfoManager : MonoBehaviour
{
    public static PlayerInfoManager Instance { get; private set; }
    
    public string PlayerName { get; set; } = "Alex";
    public string Language { get; set; } = "Filipino";
    public string Native { get; set; } = "English";
    public string Proficiency { get; set; } = "Beginner"; //Intermediate, Advanced, Expert
    public string NewWords { get; set; } = "";
    public string CurriculumRequest { get; set; } = "Speak like a native."; // Points to cover in curriculum: Current proficiency level. Error correction in target or native. How challenging. (Level 1: Very simple vocabulary, basic sentence structures (like beginner level). Level 2: Everyday vocabulary, simple compound sentences (like early intermediate). Level 3: Common vocabulary, slightly more varied sentence structures (like solid intermediate). Level 4: Less common vocabulary (beyond the most frequent words), complex sentence structures, discussion of moderately abstract topics. Level 5: Formal or very specific vocabulary, highly complex grammar, abstract or highly technical topics, potential use of literary devices.)
    public FollowerNpc follower;

    public string GetCurriculum()
    {
        return $"ONLY speak using {Language}. Use the language at a {Proficiency} level. Naturally incorporate and use these words in your dialogue: {NewWords}. {CurriculumRequest}";
    }

    public Transform GetTransform()
    {
        return transform;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        follower = FindFirstObjectByType<FollowerNpc>();
    }
}
