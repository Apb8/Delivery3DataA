//using UnityEngine;
//using Gamekit3D;

//public class TutorialDeathTracker : MonoBehaviour
//{
//    public string tutorialPhase = "final_phase";
//    public string zoneName = "Tutorial";

//    private GameMetricsSender metrics;

//    void Start()
//    {
//        metrics = FindFirstObjectByType<GameMetricsSender>();

//        Collider trigger = GetComponent<Collider>();
//        if (trigger == null)
//            trigger = gameObject.AddComponent<BoxCollider>();
//        trigger.isTrigger = true;
//    }

//    void OnTriggerEnter(Collider other)
//    {
//        if (other.CompareTag("Player"))
//        {
//            
//            Debug.Log($"Player in tutorial phase: {tutorialPhase}");
//        }
//    }

//    
//    public void RecordTutorialDeath(bool completed = false)
//    {
//        if (metrics != null)
//        {
//            GameObject player = GameObject.FindGameObjectWithTag("Player");
//            Vector3 position = player != null ? player.transform.position : transform.position;

//            metrics.RecordTutorialDeath(tutorialPhase, "tutorial", completed);
//        }
//    }

//    
//    public void CompleteTutorial()
//    {
//        RecordTutorialDeath(true);
//    }
//}