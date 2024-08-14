Steps to run:

1) Start storage emulator
2) Run the program
3) Wait a few minutes for data to gather
4) Open postman and...
   1. Search for success/failure logs: http://localhost:5001/api/queryWeatherLogs?from=YOUR_FROM_TIME&to=YOUR_TO_TIME
   2. Search for full data blob: http://localhost:5001/api/queryWeatherData?guid=YOUR_GUID_HERE
5) Enjoy browsing!

You can locate specific guids either through Azure Storage Explorer, or by first searching logs for specific timespans, then copying the guid from there.
