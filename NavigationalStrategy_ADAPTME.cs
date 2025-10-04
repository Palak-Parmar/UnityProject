using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class NavigationalStrategy_ADAPTME : MonoBehaviour
{
    [SerializeField]
    public string GroupName = "Group"; // ADAPT: add your group name here (in one word, e.g. 'Group-Anne-James-Louise').

    // Reference to boat
    [SerializeField]
    private GameObject RowBoatPrefab;

    GameObject eventManager;
    string filename = "";
    private string date;
    private string time;
    private int startingTimeMillisec;
    private float nextWriteTime = 0f;  // Add this field
    private float PrivateAngleOfLighthouseToBoat; // Easy shorthand for us to utilise the angle of the lighthouse relative to the boat which we get from the TargetLocator script.

    private List<Vector3> DangerZoneLocations; // Danger Zone positions
    private float distanceBoatToDangerZone;

    private GameObject lighthousePrefab;
    private GameObject soundObject;
    private AudioSource audioSource;
    private Vector3 lightHouseLocation;

    /* --------------------------------------------------------
     * BELOW ARE THE VARIABLES THAT CAN TURN ON SPECIFIC PARTS OF THE HARDWARE
     * TurnOnHeater variables turn on specific heaters, which are either true (= on) or false (= off).
     * The HairDryerOn variable indicates which heater is on, with only one heater being on at the same time. Hairdryers are numbered 1-8, with 1 being located to the left of heater LEFT; 2 located to the right of heater LEFT; etc.; hairdryer 8 is located on the right of heater RIGHT.
     * --------------------------------------------------------
     */

    // Bools for turning on specific heaters (as in the example)
    public bool TurnOnHeaterLEFT = false; // Turns on heaters 1 (upper) and 2 (bottom)
    public bool TurnOnHeaterLEFTMIDDLE = false; // Turns on heaters 3 (upper) and 4 (bottom)
    public bool TurnOnHeaterRIGHTMIDDLE = false; // Turns on heaters 5 (upper) and 6 (bottom)
    public bool TurnOnHeaterRIGHT = false; // Turns on heaters 7 (upper) and 8 (bottom)

    // To be used if you want to turn on specific heaters
    public bool TurnOnHeater1 = false;
    public bool TurnOnHeater2 = false;
    public bool TurnOnHeater3 = false;
    public bool TurnOnHeater4 = false;
    public bool TurnOnHeater5 = false;
    public bool TurnOnHeater6 = false;
    public bool TurnOnHeater7 = false;
    public bool TurnOnHeater8 = false;

    // Int to turn on specific hairdryers
    public int HairDryerOn = 0; // Hairdryers are indicated 1-8, with 0 being 'neutral' (= all hairdryers off).

    // Bools for auditory cues
    public bool Sound1On = false;
    public bool Sound2On = false;
    public bool Sound3On = false;
    public bool Sound4On = false;
    public bool Sound5On = false;
    public bool Sound6On = false;
    public bool Sound7On = false;
    public bool Sound8On = false;

    // Scent being sent: "Apple", "Chocolate", "Coffee", "Lavender", "Lemon", "Mint", "Grass", "Popcorn"
    public string ScentBeingSent = "";

    void Awake()
    {
        Debug.Log($"Trial started for group: {GroupName}");

        eventManager = GameObject.Find("EventManager"); // Get the GameControl object such that we can access other scripts and variables

        // General information for output documentation 
        date = DateTime.Now.ToString("dd-MM-yyyy");
        time = DateTime.Now.ToString("HH-mm-ss");
        startingTimeMillisec = (((DateTime.Now.Hour * 3600) + (DateTime.Now.Minute * 60) + DateTime.Now.Second) * 1000) + DateTime.Now.Millisecond;
        filename = Application.dataPath + "/SavedData/" + date + "--" + time + "--GroupName-" + GroupName + ".csv"; // This will be the name of your excel file in which all data is stored

        // Setting up Danger Zone locations
        DangerZoneLocations = new List<Vector3>();
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone1.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone2.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone3.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone4.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone5.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone6.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone7.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone8.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone9.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone10.transform.position);
        DangerZoneLocations.Add(eventManager.GetComponent<TargetLocator>().DangerZone11.transform.position);


        soundObject = new GameObject("soundObject");
        soundObject.AddComponent(typeof(AudioSource));
        audioSource.clip = Resources.Load(name) as AudioClip;
        lightHouseLocation = eventManager.GetComponent<TargetLocator>().lighthousePrefab.transform.position; //lighthousePrefab in TargetLocator needs to be set to Public for this
        soundObject.transform.position = lightHouseLocation;


    }

    /* CALLING SPECIFIC DANGER ZONES
     * Example of calling the location of a danger zone: 
     * eventManager.GetComponent<TargetLocator>().DangerZone1.transform.position
     */

    /* EXAMPLE OF A NAVIGATIONAL STRATEGY - THERMAL AND AIRFLOW "HOTTER / COLDER" METHOD, WITH SCENT AND AUDITORY EXAMPLE
     * Example thermal:
     * The thermal cues are used to lead a person towards the lighthouse, with the panel turning on that allows a user to point towards the lighthouse.
     * The angle at which the lighthouse is located with respect to the boat is used to determine which heater should be turned on.
     * Heaters stay continuously turned on.
     * 
     * Example airflow:
     * Note for the airflow setup: turning on one of the hairdyer immediately shuts off all other hairdryers, so we can simply adapt the number of the HairDryerOn variable so that the new hairdryer is turned on and all others shut off.
     * 
     * Example scent:
     * For this example, we turn on a different scent depending on the angle of the lighthouse, w.r.t. the boat.
     * In your own example, you can use the following scents: 1 "Apple", 2 "Chocolate", 3 "Coffee", 4 "Lavender", 5 "Lemon", 6 "Mint", 7 "Mowed grass", 8 "Popcorn"
     * You can also use your own scent. For this, please send an email.
     * 
     * Example sound: we turn on one of the sound prefabs. Note: the sound prefabs are located 360deg around the user, so the orientation
     * is different from the other modalities. If you want, you can adapt the location of the prefabs by referencing them here as is done in the 
     * SoundController.cs script and updating their position to your preferred position by changing their coordinates in the Update void.
     * Because of the 360deg positioning of the sound prefabs, moving and turning the boat does not change where the sound is coming from (after all,
     * the sound prefabs stay exactly the same relative to the boat). 
    */

    void Update()
    {
        //Debug.Log($"The angle of the lighthouse with respect to the boat is: {eventManager.GetComponent<TargetLocator>().angleOfLighthouseToBoat:F2}"); // Example of how you can check what the relative angle of the lighthouse is
        PrivateAngleOfLighthouseToBoat = eventManager.GetComponent<TargetLocator>().angleOfLighthouseToBoat; // Put it in our local shorthand. Note: this is an unnecessary step as we can continuously get the value directly from the TargetLocator script, but to avoid typo's and reference mistakes me make the transfer here.

        /* INTERPRETING THE PrivateAngleOfLighthouseToBoat VARIABLE
         * The PrivateAngleOfLighthouseToBoat variable provides a value between -180 and +180. Between -180 and 0 are the angles to the left of the boat; 0 to +180 are to the right of the boat.
         * So if we want to have the correct heater on to steer a user according to where the tower is; we can turn on specific heat sources when we have a specific angle. Because of the placement of the heaters
         * in real life (placed in a half circle in front of the user), the angles of -180 to -60 and +60 to +180 belong to the outermost left and right heater respectively.
         */

        if (!eventManager.GetComponent<TargetLocator>().EndScene) // If we are not yet at the lighthouse, keep this line in and adapt your navigational strategy below
        {
            // Add/Adapt your navigational scene wihtin this if-statement:

            if (PrivateAngleOfLighthouseToBoat > -90.0f && PrivateAngleOfLighthouseToBoat < -30.0f) // The lighthouse is on the left / left behind the user, we turn on the outermost left heater.
            {
                TurnOnHeaterLEFT = true; // This is the heater we want to turn on
                TurnOnHeaterLEFTMIDDLE = false; // All other heaters get turned off
                TurnOnHeaterRIGHTMIDDLE = false;
                TurnOnHeaterRIGHT = false;

                // Example airflow: you can turn on one of the hairdyers next to that same tower by uncommenting the line below. 
                HairDryerOn = 1;

                // Example scent: when we lean too much to the left, we give an apple scent (uncomment to use)
                ScentBeingSent = "Apple";

                // Example sound:
                Sound1On = true;
                Sound2On = false;
                Sound3On = false;
                Sound4On = false;
                Sound5On = false;
                Sound6On = false;
                Sound7On = false;
                Sound8On = false;

            }
            else if (PrivateAngleOfLighthouseToBoat > -30.0f && PrivateAngleOfLighthouseToBoat < 38.0f)
            {
                TurnOnHeaterLEFT = false;
                TurnOnHeaterLEFTMIDDLE = true; // This is the heater we want to turn on
                TurnOnHeaterRIGHTMIDDLE = true;
                TurnOnHeaterRIGHT = false;

            }


            else if (PrivateAngleOfLighthouseToBoat > 30.0f && PrivateAngleOfLighthouseToBoat < 90.0f)
            {
                TurnOnHeaterLEFT = false;
                TurnOnHeaterLEFTMIDDLE = false;
                TurnOnHeaterRIGHTMIDDLE = false;
                TurnOnHeaterRIGHT = true; // This is the heater we want to turn on

            }
        }


        /* EXAMPLE: checking if we are getting close to a danger zone
         * Based on the position of the danger zones, we check how close we are (remember that the circles have a range of 10   units).
         * I'll leave it to you to implement the right cues to avoid a user hitting a danger zone!
         */
        foreach (Vector3 DZlocation in DangerZoneLocations)
        {
            float distance = Vector3.Distance(DZlocation, RowBoatPrefab.transform.position);
            float angle = CalculateAngleToBarrier(DZlocation);

            if (distance < 40f && angle >= -90f && angle <= 90f) // Only front half
            {
                if (angle >= -90f && angle < -60f)
                { HairDryerOn = 1; } // Dryer 1
                else if (angle >= -60f && angle < 0f) { HairDryerOn = 2; } // Dryer 2 and 3
                else if (angle >= 0f && angle < 45f) { HairDryerOn = 4; } // Dryer 4 and 5
                else if (angle >= 45f && angle < 75f) { HairDryerOn = 6; } // Dryer 6 and 7    
                else if (angle >= 75f && angle <= 90f) { HairDryerOn = 8; } // Dryer 8

                //Debug.Log("We're getting really close to a danger zone...)
            }
        }
        //foreach (Vector3 DZlocation in DangerZoneLocations)
        // {
        //   distanceBoatToDangerZone = Vector3.Distance(DZlocation, RowBoatPrefab.transform.position);
        // //Debug.Log($"Danger Zone at ({dangerZonePosition.x:F2}, {dangerZonePosition.y:F2}, {dangerZonePosition.z:F2}) at distance {float distanceBoatToDangerZone:F2}");
        // if (distanceBoatToDangerZone < 40)
        //{
        //  Debug.Log("We're getting really close to a danger zone...

        if (Time.time >= nextWriteTime)
        {
            WriteCSV(); // Log the movement of the boat at every frame at once per second. 
            nextWriteTime = Time.time + 1f;  // Set next write time to 1 second from now.
        }

    }

    private float CalculateAngleToBarrier(Vector3 barrierPos)
    {
        Vector3 direction = barrierPos - RowBoatPrefab.transform.position;
        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);
        return Vector3.SignedAngle(RowBoatPrefab.transform.forward, horizontalDir, Vector3.up);
    }

    public void WriteCSV() // This void will write all relevant data to an Excel file saved in the folder 'SavedData'. This CSV is now updated at every second (see the Update() void). You can adapt this to better capture relevant data from your trial.
    {
        // First write the header of each column
        if (!File.Exists(filename))
        {
            using (TextWriter tw = new StreamWriter(filename, false))
            {
                tw.WriteLine("Date;Time;Timestamp in milliseconds;Group name;Angle of lighthouse w.r.t. the boat; Position boat - x; Position boat - y; Position boat - z");
            }
        }

        // Then write the data
        using (TextWriter tw = new StreamWriter(filename, true))
        {
            string date = DateTime.Now.ToString("dd-MM-yyyy");
            string time = DateTime.Now.ToString("HH:mm:ss");
            int TimeStampMilliseconds = (((DateTime.Now.Hour * 3600) + (DateTime.Now.Minute * 60) + DateTime.Now.Second) * 1000) + DateTime.Now.Millisecond - startingTimeMillisec; // Current time in milliseconds minus the starting time in milliseconds

            tw.WriteLine($"{date};{time};{TimeStampMilliseconds};{GroupName};{PrivateAngleOfLighthouseToBoat};{eventManager.GetComponent<TargetLocator>().updatePositionBoat.x};{eventManager.GetComponent<TargetLocator>().updatePositionBoat.y};{eventManager.GetComponent<TargetLocator>().updatePositionBoat.z}");
        }
    }





}

