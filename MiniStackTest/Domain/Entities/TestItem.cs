using Amazon.DynamoDBv2.DataModel;

namespace Domain.Entities
{
    [DynamoDBTable("FallbackTable")]
    public class TestItem
    {
        public TestItem()
        {
        }
        
        [DynamoDBHashKey]
        public string PK { get; set; }
        [DynamoDBRangeKey]
        public string SK { get; set; }
        [DynamoDBGlobalSecondaryIndexHashKey("GSI1")]
        public string GSI_PK { get; set; }
        public string Data { get; set; }

        public TestItem(string pk, string sk, string gsiPk, string largeData)
        {
            PK = pk;
            SK = sk;
            GSI_PK = gsiPk;
            Data = largeData;
        }
    }
}
