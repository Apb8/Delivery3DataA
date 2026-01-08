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
//            // Cuando el jugador entra en esta zona del tutorial
//            Debug.Log($"Player in tutorial phase: {tutorialPhase}");
//        }
//    }

//    // llamar cuando el jugador muera en el tutorial
//    public void RecordTutorialDeath(bool completed = false)
//    {
//        if (metrics != null)
//        {
//            GameObject player = GameObject.FindGameObjectWithTag("Player");
//            Vector3 position = player != null ? player.transform.position : transform.position;

//            metrics.RecordTutorialDeath(tutorialPhase, "tutorial", completed);
//        }
//    }

//    // llamar cuando complete el tutorial
//    public void CompleteTutorial()
//    {
//        RecordTutorialDeath(true);
//    }
//}