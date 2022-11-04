require('dotenv').config();
const express = require('express');
const app = express();
const rp = require('request-promise');

var ORDER_COUNTER = 0;

var port = process.env.PORT || 8080;
var targetURL = process.env.TARGETURL
var instanceName = process.env.INSTANCENAME;


/// endpoint
app.get('/', function (req, res) {
  // Read customer orders in from order list
  const customerOrders = require('./customer-orders.json');

  var o = {
    headers: {
      "Content-Type": "application/json"
    },
    resolveWithFullResponse: true,
    json: true,
    body: customerOrders[ORDER_COUNTER % 10]
  }

  console.log("Sending order to " + targetURL + " with orderID: " + customerOrders[ORDER_COUNTER % 10].customerId);

  /// post backend as specified in env var
  /// otherwise return 500
  rp.post(targetURL, o)
    .then(function (data) {
      console.log(data.statusCode + " | " + data.body);

      res.write("App " + instanceName + " calling: " + targetURL + " \n");
      res.write(data.statusCode + " status code | " + data.body);
      res.send();

      ORDER_COUNTER++;
    },
      function (err) {
        // if backend request fails
        console.log("Error calling " + process.env.INSTANCENAME + ": " + err.message)
        console.log("Status Code: 500");
        
        res.statusCode = 500;
        res.send(err);
      });
});

app.listen(port);
console.log("Get Http Endpoint running & listening on " + port + ".\nChange the port by adding an environment variable PORT.")
console.log("")
