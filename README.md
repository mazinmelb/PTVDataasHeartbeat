# PTVDataasHeartbeat
This python script uses Victorian pubic transport data made available as part of the Data Science Melbourne 2018 datathon. 
Public Transport Victoria has given us the data from myki card scanons and scanoffs, along with stop details
including latitude and longitude co-ordinates. The data is read from compressed gz datafiles and aggregated into 
30 minute time windows for only the scanons. The data comes from train, bus and tram myki data. The script treats
the normalised frequencies (in 30 minute windows) as a signal to identify peaks. This data is formatted for
visualisation as as an input to a 3d ecg heartbeat in Unity.


# Credits
This code was developed with invaluable input from Adrian Birch, and with the support of the Data Science Melbourne
datathon organisers alongside Public Transport Victoria.


