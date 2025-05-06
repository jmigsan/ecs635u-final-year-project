using UnityEngine;

public class PlayerInfoManager : MonoBehaviour
{
    public static PlayerInfoManager Instance { get; private set; }
    
    [SerializeField] string playerName = "Alex";
    [SerializeField] string language = "French";
    [SerializeField] string native = "English";
    [SerializeField] string proficiency = "Beginner"; //Intermediate, Advanced, Expert
    [SerializeField] string newWords = "Bonjour, Salut, Oui, Non, Au revoir";
    [SerializeField] string curriculumRequest = "Speak like a native."; // Points to cover in curriculum: Current proficiency level. Error correction in target or native. How challenging.

    public FollowerNpc follower;

    public string PlayerName { get => playerName; set => playerName = value; }
    public string Language { get => language; set => language = value; }
    public string Native { get => native; set => native = value; }
    public string Proficiency { get => proficiency; set => proficiency = value; }
    public string NewWords { get => newWords; set => newWords = value; }
    public string CurriculumRequest { get => curriculumRequest; set => curriculumRequest = value; }

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
