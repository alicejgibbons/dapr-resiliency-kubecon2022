using System;
using System.Text.Json.Serialization;

namespace CustomerLoyaltyJobNoLock.Model
{
#nullable enable
    public class CustomerOrder
    {
        
        public CustomerOrder(){}

        [JsonPropertyName("customerId")]
        public int CustomerId { get; set;}

        [JsonPropertyName("orderTotal")]
        public double OrderTotal { get; set; }

        [JsonPropertyName("loyaltyPoints")]
        public int? LoyaltyPoints { get; set; }
        
        [JsonPropertyName("orderCount")]
        public int OrderCount { get; set; }

        public CustomerOrder(int customerId, double orderTotal,  int orderCount, int? loyaltyPoints = 0)
        {
            CustomerId = customerId;
            OrderTotal = orderTotal;
            LoyaltyPoints = loyaltyPoints;
            OrderCount = orderCount;
        }
    }
}