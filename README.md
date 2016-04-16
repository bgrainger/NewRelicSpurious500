# New Relic .NET Agent Incorrectly Reports Internal Server Error

## Summary

When a client POSTing data to a website hosted by IIS unexpectedly
closes the connection, `http.sys` returns a 400 status code to the client,
but the New Relic .NET Agent logs a "500 Internal Server Error".

## Steps to Repro

1. Create a new "Web API Application" in Visual Studio 2015. One
can be found in this repository.
2. If desired, add an `<appSettings>` setting in `web.config` to
set the NewRelic.AppName property.
3. Begin POSTing data to `/api/values`.
4. While the server is waiting for the data to finish being uploaded, close the socket.
5. In New Relic, observe that a "500 - Internal Server Error" was reported.

One easy way to accomplish step 4 is to send an `Expect: 100-continue`
header; when the server responds with `100 Continue`, close the connection.
The following code snippet demonstrates this:

```
var data = @"POST /api/values HTTP/1.1
Accept: application/json
Content-Type: application/json
Host: test.example.com
Content-Length: 25
Expect: 100-continue

";

Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
socket.Connect("test.example.com", 80);
socket.Send(Encoding.ASCII.GetBytes(data));
var bytes = new byte[100];
socket.Receive(bytes);
// Encoding.ASCII.GetString(bytes) == "HTTP/1.1 100 Continue"
socket.Close();
```

Another way is to trigger this bug is to start POSTing data then
close the socket before all data has been uploaded.

## Details

[Microsoft Message Analyzer](https://blogs.technet.microsoft.com/messageanalyzer/)
shows that when this code is executed, the web server returns a
400 Bad Request with the following content:

```
HTTP/1.1 400 Bad Request
Content-Type: text/html; charset=us-ascii
Server: Microsoft-HTTPAPI/2.0
Date: Sat, 16 Apr 2016 05:01:10 GMT
Connection: close
Content-Length: 311

<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01//EN""http://www.w3.org/TR/html4/strict.dtd">
<HTML><HEAD><TITLE>Bad Request</TITLE>
<META HTTP-EQUIV="Content-Type" Content="text/html; charset=us-ascii"></HEAD>
<BODY><h2>Bad Request</h2>
<hr><p>HTTP Error 400. The request is badly formed.</p>
</BODY></HTML>

```

This literal string can be found in http.sys, the Windows kernel-mode
HTTP protocol stack driver. Interestingly, this response seems to
be generated at such a low level that the 400 response is not
logged to the IIS log (C:\inetpub\logs\LogFiles\W3SVC*n*\u_ex*yymmdd*.log).

However, right after this 400 response is sent, the .NET New Relic
Agent logs a 500 Internal Server Error. The New Relic agent log
will contain lines similar to the following:

```
2016-04-16 04:59:59,363 NewRelic INFO: The New Relic .NET Agent v5.17.59.0 started (pid 3368) for virtual path '/'
2016-04-16 04:59:59,410 NewRelic DEBUG: Wrapper "NewRelic.Providers.Wrapper.Asp35.IntegratedPipeline.ExecuteStepWrapper" will be used for instrumented method "System.Web.HttpApplication.ExecuteStep"
...
2016-04-16 05:01:10,396 NewRelic FINEST: Transaction created with Transaction Id: E03D87119B8FF92E, Thread Id: 7
2016-04-16 05:01:10,396 NewRelic FINEST: Setting transaction name to WebTransaction/ASP/Integrated Pipeline (priority 1 via )
2016-04-16 05:01:10,396 NewRelic FINEST: Setting transaction name to WebTransaction/Uri/api/values (priority 2 via WebTransactionUriMetricName_pseudo_factory) from WebTransaction/ASP/Integrated Pipeline (priority 1 via )
2016-04-16 05:01:10,396 NewRelic FINEST: Setting transaction name to WebTransaction/ASP/api/{controller}/{id} (priority 4 via ) from WebTransaction/Uri/api/values (priority 2 via WebTransactionUriMetricName_pseudo_factory)
2016-04-16 05:01:10,521 NewRelic FINEST: Transaction finishing with Transaction Id: E03D87119B8FF92E, Thread Id: 11
2016-04-16 05:01:10,521 NewRelic TRACE: Completed Transaction WebTransaction/ASP/api/{controller}/{id}, stack depth: 0, id: 14696841, Transaction Id: E03D87119B8FF92E, Thread Id: 11
2016-04-16 05:01:10,521 NewRelic FINEST: Transaction aborted with Transaction Id: E03D87119B8FF92E, Thread Id: 11
2016-04-16 05:01:14,505 NewRelic INFO: Harvest starting
2016-04-16 05:01:14,536 NewRelic DEBUG: Invoking "metric_data" with : ["90978450792061074",1460782814.3016024,1460782874.5211272,[[{"name":"Supportability/MetricHarvest/transmit"},[1,0.0,0.0,0.0,0.0,0.0]],[{"name":"Memory/Physical"},[6,766.1289,766.1289,126.097656,129.257813,97831.9794921875]],[{"name":"WebTransaction"},[2,0.33795719999999996,0.0,0.11794589999999999,0.2200113,0.062316207454499992]],[{"name":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.33795719999999996,0.0,0.11794589999999999,0.2200113,0.062316207454499992]],[{"name":"HttpDispatcher"},[2,0.33795719999999996,0.0,0.11794589999999999,0.2200113,0.062316207454499992]],[{"name":"WebFrontend/QueueTime"},[2,0.0312387,0.0312387,0.0,0.0312387,0.00097585632465779781]],[{"name":"ApdexAll"},[0,0,2,0.0,0.0,0]],[{"name":"Apdex"},[0,0,2,0.0,0.0,0]],[{"name":"Apdex/ASP/api/{controller}/{id}"},[0,0,2,0.0,0.0,0]],[{"name":"Supportability/RUM/Header"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"DotNet/Integrated Pipeline"},[2,0.33795719999999996,0.005395,0.11794589999999999,0.2200113,0.062316207454499992]],[{"name":"DotNet/AuthenticateRequest"},[2,0.0004521,0.0004521,0.0002208,0.00023129999999999998,1.0225232999999999E-07]],[{"name":"DotNet/AuthorizeRequest"},[2,0.00027069999999999997,0.00027069999999999997,0.00012649999999999998,0.00014419999999999998,3.6795889999999992E-08]],[{"name":"DotNet/ResolveRequestCache"},[2,0.0027478999999999997,0.0027478999999999997,0.0013,0.0014479,3.78641441E-06]],[{"name":"DotNet/MapRequestHandler"},[2,0.0001515,0.0001515,6.58E-05,8.57E-05,1.167413E-08]],[{"name":"DotNet/AcquireRequestState"},[2,0.3275883,0.3275883,0.11314719999999999,0.2144411,0.058787274237049991]],[{"name":"DotNet/LogRequest"},[2,0.00044039999999999997,0.00044039999999999997,0.0001685,0.0002719,1.0232186E-07]],[{"name":"DotNet/EndRequest"},[2,0.0009113,0.0009113,0.0002986,0.0006127,4.6456325E-07]],[{"name":"Errors/WebTransaction/ASP/api/{controller}/{id}"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Errors/allWeb"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Errors/all"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Supportability/Transactions/all"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Supportability/Transactions/allWeb"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Supportability/AnalyticsEvents/TotalEventsSeen"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"Supportability/AnalyticsEvents/TotalTransactionEventsSeen"},[2,0.0,0.0,0.0,0.0,0.0]],[{"name":"CPU/User Time"},[1,0.234375,0.234375,0.234375,0.234375,0.054931640625]],[{"name":"CPU/User/Utilization"},[1,0.00194550015,0.00194550015,0.00194550015,0.00194550015,3.7849708860449027E-06]],[{"name":"Supportability/AgentVersion/5.17.59.0"},[1,0.0,0.0,0.0,0.0,0.0]],[{"name":"Supportability/AgentVersion/desk-sea-web02/5.17.59.0"},[1,0.0,0.0,0.0,0.0,0.0]],[{"name":"DotNet/Integrated Pipeline","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.33795719999999996,0.005395,0.11794589999999999,0.2200113,0.062316207454499992]],[{"name":"DotNet/AuthenticateRequest","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.0004521,0.0004521,0.0002208,0.00023129999999999998,1.0225232999999999E-07]],[{"name":"DotNet/AuthorizeRequest","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.00027069999999999997,0.00027069999999999997,0.00012649999999999998,0.00014419999999999998,3.6795889999999992E-08]],[{"name":"DotNet/ResolveRequestCache","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.0027478999999999997,0.0027478999999999997,0.0013,0.0014479,3.78641441E-06]],[{"name":"DotNet/MapRequestHandler","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.0001515,0.0001515,6.58E-05,8.57E-05,1.167413E-08]],[{"name":"DotNet/AcquireRequestState","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.3275883,0.3275883,0.11314719999999999,0.2144411,0.058787274237049991]],[{"name":"DotNet/LogRequest","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.00044039999999999997,0.00044039999999999997,0.0001685,0.0002719,1.0232186E-07]],[{"name":"DotNet/EndRequest","scope":"WebTransaction/ASP/api/{controller}/{id}"},[2,0.0009113,0.0009113,0.0002986,0.0006127,4.6456325E-07]]]]
2016-04-16 05:01:14,646 NewRelic DEBUG: Received : {"return_value":[]}
2016-04-16 05:01:14,677 NewRelic DEBUG: Invoking "analytic_event_data" with : ["90978450792061074",[[{"errorType":"500","errorMessage":"Internal Server Error","nr.tripId":"E622F674C5BE8442","nr.pathHash":"e4f2ea70","type":"Transaction","timestamp":1460782830.4580376,"name":"WebTransaction/ASP/api/{controller}/{id}","nr.guid":"E622F674C5BE8442","duration":0.2200113,"nr.apdexPerfZone":"S","webDuration":0.2200113,"queueDuration":0.031238699999999998},{},{"response.status":"500"}],[{"errorType":"500","errorMessage":"Internal Server Error","nr.tripId":"E03D87119B8FF92E","nr.pathHash":"e4f2ea70","type":"Transaction","timestamp":1460782870.3960774,"name":"WebTransaction/ASP/api/{controller}/{id}","nr.guid":"E03D87119B8FF92E","duration":0.11794589999999999,"nr.apdexPerfZone":"S","webDuration":0.11794589999999999,"queueDuration":0.0},{},{"response.status":"500"}]]]
2016-04-16 05:01:14,786 NewRelic DEBUG: Received : {"return_value":null}
2016-04-16 05:01:14,786 NewRelic DEBUG: Invoking "error_data" with : ["90978450792061074",[[1460782830.6768014,"WebTransaction/ASP/api/{controller}/{id}","Internal Server Error","500",{"stack_trace":null,"agentAttributes":{"queue_wait_time_ms":"31.2387","response.status":"500"},"userAttributes":{},"intrinsics":{"trip_id":"E622F674C5BE8442","path_hash":"e4f2ea70"},"request_uri":"http://test.example.com/api/values"},"E622F674C5BE8442"],[1460782870.5054486,"WebTransaction/ASP/api/{controller}/{id}","Internal Server Error","500",{"stack_trace":null,"agentAttributes":{"queue_wait_time_ms":"0","response.status":"500"},"userAttributes":{},"intrinsics":{"trip_id":"E03D87119B8FF92E","path_hash":"e4f2ea70"},"request_uri":"http://test.example.com/api/values"},"E03D87119B8FF92E"]]]
2016-04-16 05:01:14,911 NewRelic DEBUG: Received : {"return_value":null}
2016-04-16 05:01:14,911 NewRelic DEBUG: Harvest finished.
```

New Relic should log a 400 Bad Request error for this response, not a
500 Internal Server Error.
