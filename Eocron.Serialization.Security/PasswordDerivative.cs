namespace Eocron.Serialization.Security
{
    public class PasswordDerivative
    {
        public byte[] Salt { get; set; }
    
        public byte[] Hash { get; set; }
    }
}