# Atea Task Nr. 2

Steps to run:

1) Start storage emulator
2) Run the program
3) Wait a few minutes for data to gather
4) Open postman and...
   1. Search for success/failure logs: http://localhost:5001/api/queryWeatherLogs?from=YOUR_FROM_TIME&to=YOUR_TO_TIME
   2. Search for full data blob: http://localhost:5001/api/queryWeatherData?guid=YOUR_GUID_HERE
5) Enjoy browsing!

Data from blob is queried by GUID, which is the rowKey for logs. You can locate specific GUIDs either through Azure Storage Explorer using rowKey, or by first searching logs for specific timespans, then copying the rowKey GUID and searching blob through Postman (or similar) using that.

TIP: When searching for logs use a date/time format YYYY-MM-DDThh:mm:ss, for example: 2024-08-17T14:40:00
