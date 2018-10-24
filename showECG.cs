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
    public float targetBPM;
    public enum levelOfDetail
    {
        daily,
        weekly,
        yearly,
        flinders
    }
    public levelOfDetail ecgType;

    private UnityEngine.Object[] info;
    private AudioSource audioData;

    private float update;
    private int freqplotTime;

    private int ixWeek;
    private int ixDay;

    private int[] selectedTimePeriods;
    private float dotYPos;

    //private float timingSecs;
    const float timingSecs = .01f; // update 30mins every .01 seconds
    // in theory this should be ??? bpm
    const float windowMins = 30f,
                lineYPos = 2;    // draw line at y pos 2


    const float timesXPos = -30, // start timescale indicators here
                timesZPos = 20;

    const int   scaleHeight = 20,
                scaleWidth = 60; // set scale of ecg display

    // todo add sunrise, sunset

    public struct Plot
    {
        public int plotNbr;
        public string plotName;
        public Vector3[] freqPlotPos;
        public int[] heartbeat;
        public DateTime dayDate;

        public Plot(int pNbr, string pName, Vector3[] freqPos, int[] hbeat, DateTime dayD)
        {
            plotNbr = pNbr;
            plotName = pName;
            freqPlotPos = freqPos;
            heartbeat = hbeat;
            dayDate = dayD;
        }
    }

    private int nbrWeeks;
    private int nbrDays;
    private int[] pBeats;

    public struct WeekInfo
    {
        public string year;
        public int weekNbr;
        public Plot[] dayPlots;
        public Plot summPlot;

        public WeekInfo(string yr, int wk, Plot[] daysP, Plot sP)
        {
            year = yr;
            weekNbr = wk;
            dayPlots = daysP;
            summPlot = sP;
        }
    }

    private WeekInfo[] allWeeks;


    void Start()
    {
        // default rotate through all weeks in 2018
        selectedTimePeriods = Enumerable.Range(26, 0).ToArray();

        // read in PTV frequency data
        allWeeks = ReadInFreqs();

        // get heartbeat sound ready to go
        audioData = gameObject.GetComponent<AudioSource>();

        if (ecgType == levelOfDetail.weekly)
        {
            // setup spatial representation of time periods
            setupTimeContext(52, "Week", "weeks");
        }
        else
        {
            setupTimeContext(52, "Week", "weeks");
            setupTimeContext(7, "Day", "days");
            ixWeek = 26;
        }

        // heartbeat usually at 16,34 so if timing is 0.015secs
        // then 60 bpm (360 bps) 
        // i can't work this out right now, todo work this out
        //timingSecs = 1/(targetBPM *60f*2f); // will wait for timingSecs before refresh
        //Debug.Log("timing is "+timingSecs);

        // initialise counters and indicies
        freqplotTime = 0;
        ixWeek = 0;
        ixDay = 0;
    }

    // Update is called once per frame
    void Update()
    {
        update += Time.deltaTime;

        if (update > timingSecs)
        {   
            if (ixWeek > nbrWeeks-1)
            {   // if at last week then start at first week again
                //Debug.Log("if at last week then start at first week again");
                ixWeek = 0;
                ixDay = 0;
                freqplotTime = 0;
            }
    
            if (ixDay >= allWeeks[ixWeek].dayPlots.Length)
            {
                ixWeek += 1;
                ixDay = 0;
                freqplotTime = 0;
            }


            if (freqplotTime == 1){
                // at start of week

                showWeekContext(allWeeks[ixWeek].weekNbr, allWeeks[ixWeek].summPlot.plotName, "weeks");
               
                string thisDay = allWeeks[ixWeek].dayPlots[ixDay].plotName;
                redrawLine(allWeeks[ixWeek].dayPlots[ixDay].freqPlotPos, "Day");
                showFreqs.text = thisDay;
                showDayContext(allWeeks[ixWeek].dayPlots[ixDay].plotNbr, allWeeks[ixWeek].dayPlots[ixDay].plotName, "days");

            }


           // move front dot point light
            moveDot(allWeeks[ixWeek].dayPlots[ixDay].freqPlotPos[freqplotTime],freqplotTime );

            pBeats = allWeeks[ixWeek].dayPlots[ixDay].heartbeat;
            if (pBeats.Contains(freqplotTime))
            {
                UpdateClock(freqplotTime);
                audioData.PlayOneShot(audioData.clip);
            }


            if (freqplotTime == (allWeeks[ixWeek].dayPlots[ixDay].freqPlotPos.Length - 1))
            {  // at the end for this day 
                // pause for a moment
                //new WaitForSecondsRealtime(timingSecs*100);
                //wnew WaitForSecondsRealtime(100);
                // click over to next plot
                ixDay = ixDay + 1;
                freqplotTime = 0;
            }


            // increment time to next 30 minute window
            freqplotTime += 1;
            update = 0.0f;

        }
    }
  


    void setupTimeContext(int nbrUnits, string prefixName, string parentName){

        float xPos;
        float zPos;

        xPos = 0;
        zPos = 50;

        Vector3 xyz_vector;
        Transform unitParent = GameObject.Find(parentName).transform;

        float unitWidth = ((scaleWidth) / 8f);
        float unitHeight = (scaleHeight * 1.5f) / nbrUnits;

        Debug.Log("lay out the "+parentName +" periods. Alll  "+ nbrUnits);
        Debug.Log("- width "+unitWidth +": height "+unitHeight );

        for (int i = 0;i<nbrUnits; i++)
        {
            Transform iUnit = Instantiate(timePrefab);
            iUnit.SetParent(unitParent, false);
            zPos += unitHeight*1.5f;
            xyz_vector = new Vector3(xPos, 0f, zPos);
            iUnit.name = string.Concat(prefixName, i);
            iUnit.localPosition = xyz_vector;
            iUnit.localScale = new Vector3(unitWidth, 1f, unitHeight);
        }

        if (parentName=="weeks")
        {
            unitHeight = 3.9f;
        }
        Transform showUnit = Instantiate(timePrefab);
        showUnit.SetParent(unitParent, false);
        showUnit.name = "show"+unitParent.name;
        xyz_vector = new Vector3(xPos+.2f, 1f, zPos+.2f);
        showUnit.localPosition = xyz_vector;
        showUnit.localScale = new Vector3(unitWidth*1.3f, 1f, unitHeight*1.1f);

    }

    void showWeekContext(int weekNbr, string showName, string parentName)
    {
        Transform thisUnit;
        Vector3 xyz_vector;

        Transform unitParent = GameObject.Find(parentName).transform;

        // move highlight block
        Transform currUnit = unitParent.GetChild(weekNbr).transform;
        xyz_vector = currUnit.localPosition;

        thisUnit = unitParent.Find("show"+ parentName).transform;
        //xyz_vector = thisUnit.localPosition;
        //xyz_vector.z = unitHeight * 1.5f *weekNbr; // move the button 
        xyz_vector.y += 1f;
        thisUnit.localPosition = xyz_vector;

        GameObject highlightUnit = GameObject.Find("currWeekName");
        TextMesh textUnit = highlightUnit.GetComponent<TextMesh>();
        textUnit.text = showName;

        Transform tUnit = highlightUnit.transform;
        // show text above week marker (fudging the position doh!)
        tUnit.localPosition = new Vector3(-59.25f, 1f, (xyz_vector.z) - 70.5f);
        // move the week name to the location of that week

        highlightUnit = GameObject.Find("currWeek");


        textUnit = highlightUnit.GetComponent<TextMesh>();
        string introName = "week "+ weekNbr.ToString();
        textUnit.text = introName;

        tUnit = highlightUnit.transform;
        // show text above week marker (fudging the position doh!)
        tUnit.localPosition = new Vector3(-59.25f, 1f, (xyz_vector.z) - 68.5f);
        // move the week name to the location of that week

    }

    void showDayContext(int dayNbr, string showName, string parentName)
    {

        Transform thisUnit;
        Vector3 xyz_vector;

        Transform unitParent = GameObject.Find(parentName).transform;

        // move highlight block
        Transform currUnit = unitParent.GetChild(dayNbr).transform;
        xyz_vector = currUnit.localPosition;

        thisUnit = unitParent.Find("show" + parentName).transform;
        //xyz_vector = thisUnit.localPosition;
        //xyz_vector.z = unitHeight * 1.5f *weekNbr; // move the button 
        xyz_vector.y += 1f;
        thisUnit.localPosition = xyz_vector;


        /*
        GameObject highlightUnit = GameObject.Find("currDay");
        TextMesh textUnit = highlightUnit.GetComponent<TextMesh>();
        textUnit.text = showName;

        Transform tUnit = highlightUnit.transform;
        // show text above week marker (fudging the position doh!)
        tUnit.localPosition = new Vector3(-48.9f, 1f, (xyz_vector.z) - 71f);
        // move the week name to the location of that week
*/
    

        showName = Enum.GetName(typeof(DayOfWeek),dayNbr).ToString();
        GameObject highlightUnit = GameObject.Find("currDayName");
        TextMesh textUnit = highlightUnit.GetComponent<TextMesh>();
        textUnit.text = showName;
        Debug.Log("show time context for " + showName);

        Transform tUnit = highlightUnit.transform;
        // show text above week marker (fudging the position doh!)
        tUnit.localPosition = new Vector3(-48.9f, 1f, (xyz_vector.z) - 72.2f);
        // move the week name to the location of that week


    }



    void redrawLine(Vector3[] plotFreqs, string lineID)
    {
        Debug.Log("find line render " + lineID);
        if (plotFreqs.Length > 0)
        {
            String lrName = "lr" + lineID;
            LineRenderer lr = GameObject.Find(lrName).GetComponent<LineRenderer>();

            lr.widthMultiplier = 1f;
            lr.positionCount = plotFreqs.Length;
            lr.SetPositions(plotFreqs);
        }
        else
        { Debug.Log("no data for " + lineID); }
    }


    void redrawLines(int ixNbr)
    {
        if (ecgType == levelOfDetail.daily)
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                redrawLine(allWeeks[ixWeek].dayPlots[(int)day].freqPlotPos, day.ToString());
            }
        }
        if (ecgType == levelOfDetail.weekly)
        {
            redrawLine(allWeeks[ixWeek].summPlot.freqPlotPos, "Week");
        }
    }


    void moveDot(Vector3 fPlotPos, int ixTime){

        GameObject myLight = GameObject.Find("dataLight");
        Transform myPos = myLight.transform;
        myLight.GetComponent<TrailRenderer>().time = timingSecs*(30);
        myLight.GetComponent<Light>().enabled = true;

       // Debug.Log("moving light dot for time " + ixTime);
        Vector3 newPos = fPlotPos;
        // put the light a bit in front of the line
        // newPos.y = dotYPos + 2f;
        // fudge for now daily
        newPos.y = newPos.y+2f;

        if (ixTime <2)
        {
            myLight.GetComponent<Light>().enabled = true;
            myLight.GetComponent<TrailRenderer>().time = 0;
        }

        myPos.localPosition = newPos;
        if (ixTime >=46)
        {
            myLight.GetComponent<Light>().enabled = false;
            myLight.GetComponent<TrailRenderer>().time = 0;
        }

    }


    WeekInfo[] ReadInFreqs(){
    
        if (ecgType == levelOfDetail.flinders ){
            info = Resources.LoadAll("flindersdata", typeof(TextAsset));}
        else {
            info = Resources.LoadAll("weeklydata", typeof(TextAsset));}

        nbrWeeks = info.Length;

        WeekInfo[] allWeeks =new WeekInfo[nbrWeeks];
        WeekInfo[] sortedAllWeeks = new WeekInfo[nbrWeeks];

        int ixWeek = 0;

        //Debug.Log("nbr of plts is " + nbrWeeks);
        foreach (TextAsset fname in info)
        {
            allWeeks[ixWeek] = readInFile(fname);
            ixWeek += 1;
        }

        // need to sort array of structs
        sortedAllWeeks = allWeeks.OrderByDescending(x => x.weekNbr).ToArray();
        return sortedAllWeeks;
    }

    WeekInfo readInFile(TextAsset freqCSV)
    {
        // columns in csv
        string thisYear,dataType;
        int thisWeekNbr;
        string thisDay,thisTime = "";
        float thisActualFreq,thisFreq,thisTimeOrdinal;
        bool isHeartbeat;

        WeekInfo thisWeek = new WeekInfo();
        thisWeek.dayPlots = new Plot[7];

        int ixDay = 6;
        int ixTime = 0; 
        int[] beats = new int[0];

        Vector3[] thisFreqPlot = new Vector3[0];
        Vector3 xyz_vector;

        dataType = "Daily";
        thisDay = "";
        string currDay = "";
        thisWeekNbr = 0; 
        thisYear = "";
        float thisYPos = lineYPos;

        string[] fLines = freqCSV.text.Split('\n');

        for (int i = 1; i < fLines.Length - 1; i++)
        {
            string[] values = fLines[i].Split(',');

            dataType = values[0];
            thisDay = values[1];
            thisActualFreq = float.Parse(values[2]);
            // nbr of 30min timewindows since midnight
            thisTimeOrdinal = float.Parse(values[3]);
            isHeartbeat = bool.Parse(values[4]);
            thisTime = values[5];
            thisWeekNbr = System.Convert.ToInt32(values[6].Substring(4));
            thisYear = values[7];
            // normalised frequency value
            thisFreq = float.Parse(values[8]);


            if (i == 1)
            {
                Debug.Log("first record "+ thisWeekNbr+" "+ thisDay);
                thisWeek.year = thisYear;
                thisWeek.weekNbr = thisWeekNbr;
                currDay = thisDay;
            }


            if (thisDay != currDay)
            {

                Debug.Log("noticed a change in date **** ");

                if (dataType == "Daily")
                {
                    {
                        Debug.Log("save daily including  ");
                        Debug.Log(" ===  date  " + currDay);
                        Debug.Log(" ===  ixDay  " + ixDay);
                        Debug.Log(" ===  int day  " + (int)DateTime.Parse(currDay).DayOfWeek);
                        Debug.Log(" ===  and positions  " + thisFreqPlot.Length);
                        //   save previous day to to weekinfo.dayPlot

                        thisWeek.dayPlots[ixDay].freqPlotPos = new Vector3[thisFreqPlot.Length];
                        thisWeek.dayPlots[ixDay].plotName = currDay;
                        thisWeek.dayPlots[ixDay].plotNbr = (int)DateTime.Parse(currDay).DayOfWeek;
                        thisWeek.dayPlots[ixDay].freqPlotPos = thisFreqPlot;
                        thisWeek.dayPlots[ixDay].heartbeat = beats;
                        // clear the arrays and start afresh
                        thisFreqPlot = new Vector3[0];
                        beats = new int[0];
                        ixDay -= 1;

                        currDay = thisDay;
                        ixTime = 0;
                    }
                }
                else // weekly
                {

                    Debug.Log("save weekly including  ");
                    thisWeek.summPlot.freqPlotPos = thisFreqPlot;
                    thisWeek.summPlot.plotName = thisDay;
                    thisWeek.summPlot.plotNbr = thisWeekNbr;
                    thisWeek.summPlot.heartbeat = beats;
                }
            }


            Debug.Log("counter is at " + i);
            // create vector for this time window and frequency
            xyz_vector = ConvertToXYZ(thisTimeOrdinal, thisYPos, thisFreq);
            thisFreqPlot = AddtoVectorArray(thisFreqPlot, xyz_vector);

            if (isHeartbeat)
            {
                Debug.Log("this is a heartbeat");
                beats = AddtoIntArray(beats, ixTime);
            }
            ixTime += 1;

        }


        return thisWeek;
    }


    Vector3 ConvertToXYZ(float myTime, float myY, float myFreq)
    {
        // math from https://stackoverflow.com/questions/5294955/how-to-scale-down-a-range-of-numbers-with-a-known-min-and-max-value//
        // xpos if x is 30 = (40 - -40)(30-0)/48 + -40
        float xPos, zPos;
        if (myTime == 0f) // if zero time then start at left of viewable screen
        { xPos = -scaleWidth * .8f / 2; }
        else // scale time from 48 values to fit into screen width
        { xPos = (myTime * scaleWidth * .8f) / 48 - (scaleWidth * .8f / 2);
        }
        if (myFreq == 0f) // if zero freq then start at bottom of viewable screen
        { zPos = -scaleHeight * .8f / 2; }
        else
        { zPos = (myFreq * scaleHeight * .8f) - (scaleHeight * .8f / 2); }
        // create vector for this time window and frequency
        return new Vector3(xPos, myY, zPos);
    }
     

    WeekInfo[] AddtoWeekInfoArray(WeekInfo[] vArray, WeekInfo newValue)
    {
        int newLength = vArray.Length + 1;
        WeekInfo[] result = new WeekInfo[newLength];
        for (int i = 0; i < vArray.Length; i++)
            result[i] = vArray[i];
        result[newLength - 1] = newValue;
        return result;
    }

    Vector3[] AddtoVectorArray(Vector3[] vArray, Vector3 newValue)
    {
        int newLength = vArray.Length + 1;
        Vector3[] result = new Vector3[newLength];
        for (int i = 0; i < vArray.Length; i++)
            result[i] = vArray[i];
        result[newLength - 1] = newValue;
        return result;
    }

    int[] AddtoIntArray(int[] array, int newValue)
    {
        int newLength = array.Length + 1;
        int[] result = new int[newLength];
        for (int i = 0; i < array.Length; i++)
            result[i] = array[i]; 
        result[newLength - 1] = newValue;
        return result;
    }

    string returnDayName(DateTime thisDate)
    {
        return Enum.GetName(typeof(DayOfWeek), thisDate.DayOfWeek).ToString();
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
