namespace PROPERTEASE.Models
{
    public class ApiResponse
    {
        public string data { get; set; }
    }

    public class eSewaResponse
    {
        public string status { get; set; }
        public string transaction_code { get; set; }
        public string transaction_uuid { get; set; }
        public string total_amount { get; set; }
        public string product_code { get; set; }
        public string signed_field_names { get; set; }
        public string signature { get; set; }
    }

    public enum PaymentMethod
    {
        eSewa
    }

    public enum PaymentVersion
    {
        v2
    }

    public enum PaymentMode
    {
        Sandbox,
        Live
    }
}