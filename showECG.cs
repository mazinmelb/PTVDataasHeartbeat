using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class showECG : MonoBehaviour
{
    public TextAsset sunCSV;
    public Text showTime;
    public Text showFreqs;
    public Transform timePrefab;

    private UnityEngine.Object[] info;
    private AudioSource audioData;

    private float update;
    private int freqplotTime;
    private int ixPlot;

    const float timingSecs = .015f, // update 30mins every .01 seconds
                windowMins = 30f,
                lineYPos = 2;    // draw line at y pos 2
    const int   scaleHeight = 20,
                scaleWidth = 80; // set scale of ecg display

    public struct Plot
    {
        public int plotNbr;
        public string plotName;
        public Vector3[] freqPlotPos;
        public int[] heartbeat;
        public int weekNbr;
        public string year;

        public Plot(int pNbr, string pName, Vector3[] freqPos, int[] hbeat, int wk, string yr)
        {
            plotNbr=pNbr;
            plotName=pName;
            freqPlotPos=freqPos;
            heartbeat=hbeat;
            weekNbr = wk;
            year = yr;
        }
    }


    private Plot[] allPlots;
    private int nbrPlots;


    // Map the PTV stops
    void Start()
    {
        showTime.text = "00:00";
        ixPlot = 0;

        allPlots = readInFreqs();
        freqplotTime = 0;

        audioData = gameObject.GetComponent<AudioSource>();

        Debug.Log("Create line for first plot " + allPlots[ixPlot].plotName);
        createLine(allPlots[ixPlot].freqPlotPos);

        setupTimeUnits(52,"Week","weeks");
        //setupTimeUnits(7, "Day", "days");


    }

    // Update is called once per frame
    void Update()
    {
        update += Time.deltaTime;

        int[] pBeats = allPlots[ixPlot].heartbeat;

        if (update > timingSecs)
        {
            // hard code nbr of intervals for proof of concept
            if (freqplotTime > 48) {
                if (ixPlot == nbrPlots) { ixPlot = 0; }
                else {ixPlot = ixPlot + 1;}
                createLine(allPlots[ixPlot].freqPlotPos);
                updateTimeUnits(allPlots[ixPlot].weekNbr, allPlots[ixPlot].plotName );
                // showFreqs.text = allPlots[ixPlot].plotName;
                freqplotTime = 0;
            }
            else{
                freqplotTime += 1;   
            }

            if (pBeats.Contains(freqplotTime)) {
                audioData.PlayOneShot(audioData.clip);
                // sound from Lunardrive on freesound
                Debug.Log("play clip at " + freqplotTime);
            }
            else if (freqplotTime==48){
                new WaitForSecondsRealtime(timingSecs*100);
                // pause for a moment
            }

            update = 0.0f;
            UpdateClock(freqplotTime);
            moveDot(allPlots[ixPlot].freqPlotPos,freqplotTime);
            // moveSun();
        }
    }

    void updateTimeUnits(int weekNbr, string showName ){

        Debug.Log("moving highlight text to position x");
        string fullName = "Week" + (weekNbr.ToString());
        Debug.Log("find game object " + fullName);
        Transform thisUnit = GameObject.Find(fullName).transform;
        Vector3 xyz_vector = thisUnit.localPosition;
        Debug.Log(xyz_vector.x);
        GameObject highlightUnit = GameObject.Find("currWeek");
        TextMesh textUnit = highlightUnit.GetComponent<TextMesh>();
        textUnit.text = showName;
        Transform tUnit = GameObject.Find("currWeek").transform;
        // show text above week marker
        xyz_vector.z += 5f;
        tUnit.localPosition = xyz_vector;
        // move the week name to the location of that week

    }

    void setupTimeUnits(int nbr, string prefixName, string parentName){

        Debug.Log("lay out the weeks: width, height");
        Vector3 xyz_vector;
        Transform unitParent = GameObject.Find(parentName).transform;

        float unitWidth = ((scaleWidth * .8f) / nbr);
        float unitHeight = (scaleHeight * .8f) /5;

        for (int i = 0; i < nbr; i++)
        {
            Transform iUnit = Instantiate(timePrefab);
            iUnit.SetParent(unitParent, false);
            xyz_vector = new Vector3(i * unitWidth, 0f,0f);
            iUnit.name = string.Concat(prefixName, i);
            iUnit.localPosition = xyz_vector;
            iUnit.localScale = new Vector3(unitWidth*.95f, 1f, unitHeight);
        }
    }


    void createLine(Vector3[] plotFreqs)
    {

        Vector3[] dayPositions = new Vector3[48];
        LineRenderer lr = GameObject.Find("dataLine").GetComponent<LineRenderer>();

        for (var t = 0; t < 48;t++)
        {
            Debug.Log(t);
            dayPositions[t] = plotFreqs[t];
            Debug.Log(dayPositions[t]);

        }

        lr.widthMultiplier = 1f;
        lr.positionCount = 48;
        lr.SetPositions(dayPositions); 
    }


    void moveSun() { }

    void moveDot(Vector3[] plotFreq,int ixTime){

        GameObject myLight = GameObject.Find("dataLight");
        Transform myPos = myLight.transform;
        myLight.GetComponent<TrailRenderer>().time = timingSecs*30;
        myLight.GetComponent<Light>().enabled = true;
 
        if (ixTime < 48)
        {
            Vector3 newPos = plotFreq[ixTime];
            newPos.y = lineYPos * 1.5f;
            myPos.localPosition = newPos;

        }
        else {
            myLight.GetComponent<Light>().enabled = false;
            myLight.GetComponent<TrailRenderer>().time = 0;
        }

       }

    Plot[] readInFreqs(){
        //string fname = "summaryWeek17";
        info = Resources.LoadAll("weeklydata", typeof(TextAsset));
        nbrPlots = info.Length;

        Plot[] allPlots=new Plot[nbrPlots+1];

        int ixPlot = 0;

        Debug.Log("nbr of plts is " + nbrPlots);
        foreach (TextAsset fname in info)
        {
            allPlots[ixPlot] = readInFile(fname);
            ixPlot += 1;
        }

        // need to sort array of structs
        //Array.Sort<Plot>(allPlots, (x, y) => x.weekNbr.CompareTo(y.weekNbr));
        allPlots.OrderByDescending(x => x.weekNbr);

        return allPlots;
    }


    Plot readInFile(TextAsset freqCSV)
    {
        // variables are declared in order of columns in csv
        string thisYear;
        int thisWeek;
        string thisDay,thisTime = "";
        float thisActualFreq,thisFreq,thisTimeOrdinal;
        bool isHeartbeat;

        Plot thisPlot = new Plot();
        thisPlot.plotName = "";

        float xPos, zPos;
        int ixTime = 0;
        int[] beats = new int[0];
        Vector3[] freqPlotPos = new Vector3[50];

        thisDay = "";
        thisWeek =0; 
        thisYear = "";
        // assuming csv is sorted by date time
        // and assuming all dates are consecutive (no missing days)
        // and assuming there are freq values for every time window (30 mins) and it could be 0

        string[] fLines = freqCSV.text.Split('\n');


        for (int i = 1; i < fLines.Length - 1; i++)
        {
            string[] values = fLines[i].Split(',');


            thisYear = values[0];
            thisWeek = System.Convert.ToInt32( values[1].Substring(4));
            thisDay = values[2];
            thisTime = values[3];
            thisActualFreq = float.Parse(values[4]);
            thisFreq = float.Parse(values[5]);
            thisTimeOrdinal = float.Parse(values[6]);
            isHeartbeat = bool.Parse(values[7]);
            ixTime = (int)thisTimeOrdinal;

            // math from https://stackoverflow.com/questions/5294955/how-to-scale-down-a-range-of-numbers-with-a-known-min-and-max-value//
            // xpos if x is 30 = (40 - -40)(30-0)/48 + -40

            // todo what is 0 position?
            if (thisTimeOrdinal == 0f){
                xPos = -scaleWidth * .8f / 2;
            } else {
                xPos = (thisTimeOrdinal * scaleWidth * .8f) / 48 - (scaleWidth * .8f / 2); // scale time from 48 values to fit into screen width
            }
            if (thisFreq == 0f){
                zPos = -scaleHeight * .8f / 2;
            } else {
                zPos = (thisFreq * scaleHeight*.8f) - (scaleHeight* .8f/ 2); 
            }
                     
            // create vector for this day and this time window
            freqPlotPos[ixTime] = new Vector3(xPos, lineYPos, zPos);

            if (isHeartbeat ){
                Debug.Log("heartbeat at " + ixTime);
                beats = AddtoArray(beats,ixTime);
            }
        }
        thisPlot.plotName = thisDay;
        thisPlot.heartbeat = beats;
        thisPlot.freqPlotPos = freqPlotPos;
        thisPlot.weekNbr = thisWeek;
        thisPlot.year = thisYear;

        return thisPlot;
 
    }

    int[] AddtoArray(int[] array, int newValue)
    {
        int newLength = array.Length + 1;

        int[] result = new int[newLength];
        for (int i = 0; i < array.Length; i++)
            result[i] = array[i]; 

        result[newLength - 1] = newValue;
        return result;
    }

    void UpdateClock(int ixTime)
    {
        string timeTxt;
        int totalMinutes = ixTime * (int)windowMins;
        int justDays = Mathf.FloorToInt(totalMinutes / (60 * 24));
        totalMinutes -= justDays * 60 * 24;
        int justHrs = Mathf.FloorToInt(totalMinutes / 60);
        totalMinutes -= justHrs * 60;
        int justMins = (totalMinutes);
        timeTxt = justHrs.ToString("D2") + ":" + justMins.ToString("D2");

        // show current time in format 15:30
        showTime.text = timeTxt;
    }
      


    void readInSunsetSunrise(string[] theseDays)
    {

        Debug.Log("Strt to read in sun melbtimes file");

        float sunrise, sunset, lenDay;
        string thisDay;

        string[] fLines = sunCSV.text.Split('\n');

        for (int i = 1; i < fLines.Length; i++)
        {
            string[] values = fLines[i].Split(',');
            if (values.Length > 1)
            {
                thisDay = (values[0]);
                if (System.Array.IndexOf(theseDays, thisDay) >= 0)
                {
                    sunrise = float.Parse(values[1]);
                    sunset = float.Parse(values[2]);

                    // sunrise - sun is at eastern most point translate longitude (145.4f) to x
                    // sunrise at time sun location is eastlimit
                    // middaysun at time sun location is centre, at middayHeight
                    // sunset at time sun location is west limit
                    // move every half hour location (180 degrees / len)
                    // 180 degress for sunrise to sunset, eg lenDay is 800mins then
                    // 180*30mins/800 = 6.75 degrees every time window (half hour)

                    lenDay = sunset - sunrise;
                    float sunriseToSunset = 180.0f * (windowMins) / lenDay;

                    // time spent dark for today is midnight to sunrise
                    // 90*30mins/ 361 (eg mins to sunrise) = 15 degrees
                    // and then sunset to midnight
                    // 90*30mins/ 195 (eg mins to midnight) = 14 degrees
                    float mnightToSunrise = 90.0f * (windowMins * 60) / sunrise;
                    float lenNight = (24 * 60) - sunset;
                    float sunsetToMnight = 90.0f * (windowMins * 60) / lenNight;
                    /*
                    Debug.Log("Today is ");
                    Debug.Log(thisDay);
                    Debug.Log("sun day, rise, set");
                    Debug.Log(thisDay);
                    Debug.Log(sunrise);
                    Debug.Log(sunset);
                    Debug.Log("move by degrees (minight - to sunrise, sunrise to sunset, sunset to midngiht");
                    Debug.Log(mnightToSunrise);
                    Debug.Log(sunriseToSunset);
                    Debug.Log(sunsetToMnight);
                    */
                    /*
                    Vector3 xyz_vector = new Vector3(lonToX(eastlimit), 0, 0);
                    Transform iSun = Instantiate(sunPrefab);
                    iSun.localPosition = xyz_vector; */

                    // to-do
                    // fill array from 12:01am 24-jun to 11:59pm 31-june
                    // each line is 30 minutes afer 12:00am 24 Jun
                    // each line is vector for sun's position

                    //           sunPos = 0;
                    // iSun.localScale = new Vector3(.5f, .5f, .5f);
                }

            }
        }

    }
}
