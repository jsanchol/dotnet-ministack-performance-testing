using Amazon.DynamoDBv2.DataModel;

namespace Domain.Entities
{
    [DynamoDBTable("TestTableGSIProvisioned")]
    public class TestItem
    {
        [DynamoDBHashKey]
        public string PK { get; set; }
        [DynamoDBRangeKey]
        public string SK { get; set; }
        [DynamoDBGlobalSecondaryIndexHashKey("GSI")]
        public string GSI_PK { get; set; }
        public string LargeData { get; set; }

        public TestItem(string pk, string sk, string gsiPk, string largeData)
        {
            PK = pk;
            SK = sk;
            GSI_PK = gsiPk;
            LargeData = largeData;
        }
    }
}
