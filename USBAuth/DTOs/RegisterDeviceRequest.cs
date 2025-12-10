namespace DTOs
{
    public class RegisterDeviceRequest
    {
        public string DeviceId { get; set; } = default!;
        public string OwnerName { get; set; } = default!;
        public string PublicKeyPem { get; set; } = default!;
        public string Pin { get; set; } = default!;
        public string RegistrationSecret { get; set; } = default!;
    }
}
